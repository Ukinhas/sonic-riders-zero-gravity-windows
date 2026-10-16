#define NOMINMAX
#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0A00
#include <windows.h>
#include <shlobj.h>
#include <compressapi.h>
#include <bcrypt.h>
#include <stdint.h>
#include <wchar.h>

// One executable for delivery. Assets are unpacked once to a versioned cache.
// The original disc is never accessed on the destination machine.
struct Item { wchar_t path[160]; uint64_t size, offset, packed; unsigned char hash[32]; };
static Item items[128];
static HWND window, label;
static bool cancelled;
static uint64_t total, completed;
static BCRYPT_ALG_HANDLE sha;
static DWORD objectSize;
static void fail(const wchar_t *message) { MessageBoxW(nullptr, message, L"Sonic Riders Zero Gravity", MB_OK | MB_ICONERROR); ExitProcess(1); }
static void require(bool condition, const wchar_t *message) { if (!condition) fail(message); }
static bool read(HANDLE file, void *data, DWORD size) { DWORD got = 0; return ReadFile(file, data, size, &got, nullptr) && got == size; }
static bool write(HANDLE file, const void *data, DWORD size) { DWORD got = 0; return WriteFile(file, data, size, &got, nullptr) && got == size; }
static bool seek(HANDLE file, uint64_t offset) { LARGE_INTEGER pos; pos.QuadPart = offset; return SetFilePointerEx(file, pos, nullptr, FILE_BEGIN); }
static uint64_t u64(const unsigned char *p) { uint64_t value = 0; for (int i = 7; i >= 0; --i) value = (value << 8) | p[i]; return value; }
static uint32_t u32(const unsigned char *p) { return (uint32_t)p[0] | ((uint32_t)p[1]<<8) | ((uint32_t)p[2]<<16) | ((uint32_t)p[3]<<24); }
static bool equal(const unsigned char *a, const unsigned char *b, unsigned n) { for (unsigned i=0;i<n;++i) if(a[i]!=b[i]) return false; return true; }
static BCRYPT_HASH_HANDLE hashStart(void *object) { BCRYPT_HASH_HANDLE h; require(BCryptCreateHash(sha,&h,(PUCHAR)object,objectSize,nullptr,0,0)>=0,L"Cannot initialize integrity check."); return h; }
static void hashFinish(BCRYPT_HASH_HANDLE h, const unsigned char *expected) { unsigned char sum[32]; require(BCryptFinishHash(h,sum,32,0)>=0 && equal(sum,expected,32),L"Game package integrity check failed. Copy the complete executable again."); BCryptDestroyHash(h); }
static LRESULT CALLBACK events(HWND h, UINT msg, WPARAM w, LPARAM l) { if(msg==WM_CLOSE) { cancelled=true; return 0; } return DefWindowProcW(h,msg,w,l); }
static void progress(const wchar_t *name) {
    wchar_t text[512];
    swprintf(text,512,L"Preparing game files: %u%%\n\n%ls\n\nThis happens only on the first opening.\nYour saved progress is kept separately.", total ? (unsigned)(completed*100/total) : 0,name);
    SetWindowTextW(label,text);
    MSG msg; while(PeekMessageW(&msg,nullptr,0,0,PM_REMOVE)) { TranslateMessage(&msg); DispatchMessageW(&msg); }
    if(cancelled) ExitProcess(0);
}
static void folders(wchar_t *path) { for(wchar_t *p=path+3;*p;++p) if(*p==L'\\') { *p=0; CreateDirectoryW(path,nullptr); *p=L'\\'; } }
int WINAPI wWinMain(HINSTANCE instance,HINSTANCE,PWSTR,int) {
    HANDLE lock=CreateMutexW(nullptr,FALSE,L"Local\\SRZGStandalonePrepare");
    require(lock && WaitForSingleObject(lock,INFINITE)!=WAIT_FAILED,L"Cannot prepare game files.");
    wchar_t self[32768]{};
    DWORD len=GetModuleFileNameW(nullptr,self,32768);
    require(len && len<32768,L"Cannot locate the executable.");
    HANDLE file=CreateFileW(self,GENERIC_READ,FILE_SHARE_READ,nullptr,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,nullptr);
    require(file!=INVALID_HANDLE_VALUE,L"Cannot read the game package.");
    LARGE_INTEGER fileSize;
    require(GetFileSizeEx(file,&fileSize) && fileSize.QuadPart>56,L"Incomplete game package.");
    unsigned char footer[56];
    require(seek(file,fileSize.QuadPart-56) && read(file,footer,56) && equal(footer,(const unsigned char *)"SRZGPK02",8),L"Missing game data in this executable.");
    uint64_t indexOffset=u64(footer+8),indexSize=u64(footer+16);
    require(indexSize>=12 && indexSize<65536 && indexOffset<(uint64_t)fileSize.QuadPart && indexSize==(uint64_t)fileSize.QuadPart-56-indexOffset,L"Invalid game package directory.");
    auto index=(unsigned char *)HeapAlloc(GetProcessHeap(),0,(SIZE_T)indexSize);
    require(index && seek(file,indexOffset) && read(file,index,(DWORD)indexSize),L"Cannot read game package directory.");
    DWORD got;
    require(BCryptOpenAlgorithmProvider(&sha,BCRYPT_SHA256_ALGORITHM,nullptr,0)>=0 && BCryptGetProperty(sha,BCRYPT_OBJECT_LENGTH,(PUCHAR)&objectSize,sizeof objectSize,&got,0)>=0,L"SHA256 is unavailable.");
    void *hashObject=HeapAlloc(GetProcessHeap(),0,objectSize);
    require(hashObject!=nullptr,L"Not enough memory.");
    auto hash=hashStart(hashObject);
    require(BCryptHashData(hash,index,(ULONG)indexSize,0)>=0,L"Cannot check package directory.");
    hashFinish(hash,footer+24);
    require(equal(index,(const unsigned char *)"SRZGIDX2",8),L"Invalid package index.");
    unsigned count=u32(index+8); require(count>0 && count<=128,L"Invalid package entry count.");
    uint64_t at=12;
    for(unsigned i=0;i<count;++i) {
        require(at+2<=indexSize,L"Truncated package directory.");
        unsigned length=index[at] | (index[at+1]<<8); at+=2;
        require(length>0 && length<150 && at+length+56<=indexSize,L"Invalid package path.");
        for(unsigned j=0;j<length;++j) {
            unsigned char c=index[at+j];
            require((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='-'||c=='_'||c=='.'||c=='+'||(c=='/'&&j>0),L"Invalid package filename.");
            require(!(c=='.'&&j+1<length&&index[at+j+1]=='.'),L"Invalid package path.");
            items[i].path[j]=c=='/'?L'\\':c;
        }
        at+=length;
        items[i].size=u64(index+at); items[i].offset=u64(index+at+8); items[i].packed=u64(index+at+16);
        for(unsigned j=0;j<32;++j) items[i].hash[j]=index[at+24+j];
        at+=56;
        require(items[i].offset<indexOffset && items[i].packed<=indexOffset-items[i].offset,L"Invalid package extent.");
        require(items[i].size<8ull*1024*1024*1024,L"Invalid asset size.");
        total+=items[i].size;
    }
    require(at==indexSize,L"Unexpected package directory data.");
    PWSTR local=nullptr;
    require(SUCCEEDED(SHGetKnownFolderPath(FOLDERID_LocalAppData,0,nullptr,&local)),L"Cannot locate application data folder.");
    wchar_t version[33]{}; const wchar_t hex[]=L"0123456789abcdef";
    for(unsigned i=0;i<16;++i) { version[i*2]=hex[footer[24+i]>>4]; version[i*2+1]=hex[footer[24+i]&15]; }
    wchar_t base[32768],ready[32768],target[32768];
    require(wcslen(local)<32000,L"Application data path is too long.");
    swprintf(base,32768,L"%ls\\SonicRidersZeroGravity\\app-%ls",local,version); CoTaskMemFree(local);
    swprintf(ready,32768,L"%ls\\ready",base);
    bool cached=GetFileAttributesW(ready)!=INVALID_FILE_ATTRIBUTES;
    for(unsigned i=0;cached&&i<count;++i) {
        swprintf(target,32768,L"%ls\\%ls",base,items[i].path);
        WIN32_FILE_ATTRIBUTE_DATA info;
        cached=GetFileAttributesExW(target,GetFileExInfoStandard,&info) && (((uint64_t)info.nFileSizeHigh<<32)|info.nFileSizeLow)==items[i].size;
    }
    if(!cached) {
        folders(ready); CreateDirectoryW(base,nullptr);
        ULARGE_INTEGER available;
        require(GetDiskFreeSpaceExW(base,&available,nullptr,nullptr) && available.QuadPart>total+128*1024*1024,L"Not enough free space. Allow at least 5 GB for game files.");
        WNDCLASSW cls{}; cls.hInstance=instance; cls.lpfnWndProc=events; cls.lpszClassName=L"SRZGPrepare"; cls.hCursor=LoadCursor(nullptr,IDC_ARROW); cls.hbrBackground=(HBRUSH)(COLOR_WINDOW+1); RegisterClassW(&cls);
        window=CreateWindowW(cls.lpszClassName,L"Sonic Riders Zero Gravity",WS_OVERLAPPED|WS_CAPTION|WS_SYSMENU,CW_USEDEFAULT,CW_USEDEFAULT,530,220,nullptr,nullptr,instance,nullptr);
        label=CreateWindowW(L"STATIC",L"Preparing game files...",WS_CHILD|WS_VISIBLE,24,24,470,145,window,nullptr,instance,nullptr);
        ShowWindow(window,SW_SHOW); UpdateWindow(window);
        DECOMPRESSOR_HANDLE decompressor;
        require(CreateDecompressor(COMPRESS_ALGORITHM_XPRESS_HUFF,nullptr,&decompressor),L"Cannot initialize decompression.");
        auto packed=(unsigned char *)HeapAlloc(GetProcessHeap(),0,2*1024*1024);
        auto raw=(unsigned char *)HeapAlloc(GetProcessHeap(),0,1024*1024);
        require(packed&&raw,L"Not enough memory to prepare game files.");
        for(unsigned i=0;i<count;++i) {
            progress(items[i].path);
            swprintf(target,32768,L"%ls\\%ls",base,items[i].path); folders(target);
            wchar_t temporary[32768]; swprintf(temporary,32768,L"%ls.part",target);
            HANDLE output=CreateFileW(temporary,GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
            require(output!=INVALID_HANDLE_VALUE && seek(file,items[i].offset),L"Cannot create game file.");
            hash=hashStart(hashObject); uint64_t left=items[i].size,consumed=0;
            while(left) {
                unsigned char header[8]; require(read(file,header,8),L"Truncated game data."); consumed+=8;
                uint32_t rawSize=u32(header),packedSize=u32(header+4),stored=packedSize?packedSize:rawSize;
                require(rawSize>0 && rawSize<=1024*1024 && rawSize<=left && stored<=2*1024*1024 && consumed<=items[i].packed && stored<=items[i].packed-consumed,L"Invalid game data block.");
                require(read(file,packed,stored),L"Cannot read game data."); consumed+=stored;
                SIZE_T actual=0;
                if(packedSize) require(Decompress(decompressor,packed,packedSize,raw,rawSize,&actual)&&actual==rawSize,L"Cannot decompress game data.");
                else CopyMemory(raw,packed,rawSize);
                require(write(output,raw,rawSize) && BCryptHashData(hash,raw,rawSize,0)>=0,L"Cannot write game files.");
                left-=rawSize; completed+=rawSize; progress(items[i].path);
            }
            require(consumed==items[i].packed,L"Invalid game asset length.");
            hashFinish(hash,items[i].hash); require(FlushFileBuffers(output),L"Cannot save game file."); CloseHandle(output);
            require(MoveFileExW(temporary,target,MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH),L"Cannot finish preparing game files. Close any running copy of the game and retry.");
        }
        CloseDecompressor(decompressor); HeapFree(GetProcessHeap(),0,packed); HeapFree(GetProcessHeap(),0,raw);
        HANDLE marker=CreateFileW(ready,GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
        require(marker!=INVALID_HANDLE_VALUE,L"Cannot save preparation status."); CloseHandle(marker); DestroyWindow(window);
    }
    CloseHandle(file); HeapFree(GetProcessHeap(),0,index); HeapFree(GetProcessHeap(),0,hashObject); BCryptCloseAlgorithmProvider(sha,0);
    swprintf(target,32768,L"%ls\\SonicRidersZeroGravity.exe",base);
    wchar_t command[32768]; swprintf(command,32768,L"\"%ls\"",target);
    STARTUPINFOW startup{}; startup.cb=sizeof startup; PROCESS_INFORMATION process{};
    require(CreateProcessW(target,command,nullptr,nullptr,FALSE,0,nullptr,base,&startup,&process),L"Cannot start the game. Check that Windows and graphics drivers meet the requirements.");
    CloseHandle(process.hThread); CloseHandle(process.hProcess); ReleaseMutex(lock); CloseHandle(lock);
    return 0;
}

