#include <windows.h>
#include <commdlg.h>
#include <shlobj.h>
#include <shellapi.h>

#include <algorithm>
#include <cstdio>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <iterator>
#include <stdexcept>
#include <string>
#include <vector>

int sonic_main(int argc, char **argv);
namespace fs = std::filesystem;

namespace {
std::string utf8(const fs::path &path) {
    const auto value = path.u8string();
    return {reinterpret_cast<const char *>(value.data()), value.size()};
}

void notice(const wchar_t *text) {
    MessageBoxW(nullptr, text, L"Sonic Riders Zero Gravity", MB_OK | MB_ICONINFORMATION);
}

fs::path select_iso() {
    wchar_t filename[32768]{};
    OPENFILENAMEW dialog{};
    dialog.lStructSize = sizeof dialog;
    dialog.lpstrFilter = L"PS2 disc image (*.iso)\0*.iso\0\0";
    dialog.lpstrFile = filename;
    dialog.nMaxFile = static_cast<DWORD>(std::size(filename));
    dialog.lpstrTitle = L"Select Sonic Riders Zero Gravity USA (SLUS_216.42) ISO";
    dialog.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
    return GetOpenFileNameW(&dialog) ? fs::path(filename) : fs::path();
}

// Accept only the executable used to generate this build. No ISO is modified.
bool matches_disc(const fs::path &iso, const fs::path &elf) {
    std::ifstream disc(iso, std::ios::binary), original(elf, std::ios::binary);
    if (!disc || !original) return false;
    unsigned char pvd[2048]{};
    disc.seekg(16 * 2048);
    if (!disc.read(reinterpret_cast<char *>(pvd), sizeof pvd) || std::memcmp(pvd, "\1CD001", 6)) return false;
    const auto u32 = [](const unsigned char *p) { return unsigned(p[0]) | (unsigned(p[1]) << 8) | (unsigned(p[2]) << 16) | (unsigned(p[3]) << 24); };
    const auto root_lba = u32(pvd + 158), root_size = u32(pvd + 166);
    if (!root_size || root_size > 16 * 1024 * 1024) return false;
    std::vector<unsigned char> root(root_size);
    disc.seekg(static_cast<std::streamoff>(root_lba) * 2048);
    if (!disc.read(reinterpret_cast<char *>(root.data()), root.size())) return false;
    for (size_t at = 0; at < root.size();) {
        unsigned length = root[at];
        if (!length) { at = (at / 2048 + 1) * 2048; continue; }
        if (length < 34 || at + length > root.size() || root[at + 32] > length - 33) return false;
        const unsigned char *record = root.data() + at;
        const std::string name(reinterpret_cast<const char *>(record + 33), record[32]);
        if (name == "SLUS_216.42;1" || name == "SLUS_216.42") {
            const auto size = u32(record + 10);
            if (size != fs::file_size(elf)) return false;
            std::vector<char> data(size), expected(size);
            disc.seekg(static_cast<std::streamoff>(u32(record + 2)) * 2048);
            if (!disc.read(data.data(), size) || !original.read(expected.data(), size)) return false;
            return data == expected;
        }
        at += length;
    }
    return false;
}
}

int WINAPI wWinMain(HINSTANCE, HINSTANCE, PWSTR, int) {
    try {
        // Explicit developer harness; all paths (including test card) are supplied.
        int test_count = 0;
        LPWSTR *test_command = CommandLineToArgvW(GetCommandLineW(), &test_count);
        if (test_command && test_count > 2 && wcscmp(test_command[1], L"--diagnostic") == 0) {
            std::vector<std::string> test_args{utf8(fs::path(test_command[0]))};
            for (int i = 2; i < test_count; ++i) test_args.push_back(utf8(fs::path(test_command[i])));
            LocalFree(test_command);
            std::vector<char *> test_argv;
            for (auto &arg : test_args) test_argv.push_back(arg.data());
            test_argv.push_back(nullptr);
            return sonic_main(static_cast<int>(test_args.size()), test_argv.data());
        }
        if (test_command) LocalFree(test_command);
        // A second instance must not write to the same virtual memory card.
        HANDLE single = CreateMutexW(nullptr, FALSE, L"Local\\SonicRidersZeroGravityNative");
        if (single && GetLastError() == ERROR_ALREADY_EXISTS) {
            CloseHandle(single);
            notice(L"The game is already open.");
            return 0;
        }
        wchar_t executable[32768]{};
        const DWORD count = GetModuleFileNameW(nullptr, executable, static_cast<DWORD>(std::size(executable)));
        if (!count || count >= std::size(executable)) throw std::runtime_error("Cannot locate application folder");
        const fs::path base = fs::path(executable).parent_path();
        const fs::path elf = base / L"data" / L"SLUS_216.42";
        if (!fs::exists(elf)) {
            notice(L"Missing data\\SLUS_216.42. Extract the complete application folder before starting.");
            return 1;
        }
        PWSTR local = nullptr;
        if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &local))) throw std::runtime_error("Cannot locate save folder");
        const fs::path save = fs::path(local) / L"SonicRidersZeroGravity";
        CoTaskMemFree(local);
        fs::create_directories(save);
        const fs::path settings = save / L"iso-path.txt";
        const fs::path card = save / L"Mcd001.ps2";
        // Keep the user's progress before enabling the content-unlock patch.
        const fs::path backup = save / L"Mcd001-before-unlock.ps2";
        if (fs::exists(card) && !fs::exists(backup)) fs::copy_file(card, backup);
#ifdef PS2_STANDALONE
        const fs::path iso = base / L"data" / L"game.dat";
        if (!matches_disc(iso, elf)) {
            notice(L"The bundled game data is missing or damaged. Open the standalone executable again to restore the application files.");
            return 1;
        }
#else
        fs::path iso;
        int command_count = 0;
        LPWSTR *command = CommandLineToArgvW(GetCommandLineW(), &command_count);
        if (command && command_count == 2) iso = command[1];
        if (command) LocalFree(command);
        if (iso.empty() && fs::exists(base / L"game.iso")) iso = base / L"game.iso";
        if (iso.empty()) {
            std::ifstream input(settings, std::ios::binary);
            std::string previous;
            std::getline(input, previous);
            if (!previous.empty()) iso = fs::u8path(previous);
        }
        while (iso.empty() || !matches_disc(iso, elf)) {
            if (!iso.empty()) notice(L"This ISO is unavailable or does not match the USA SLUS_216.42 executable required by this build. Please select the matching ISO.");
            iso = select_iso();
            if (iso.empty()) return 0;
        }
        std::ofstream(settings, std::ios::binary | std::ios::trunc) << utf8(iso) << '\n';
#endif
        // The optional initial card is English. Never replace existing progress.
        if (!fs::exists(card) && fs::exists(base / L"data" / L"initial-card.ps2"))
            fs::copy_file(base / L"data" / L"initial-card.ps2", card);
        _wfreopen((save / L"game.log").c_str(), L"w", stderr);
        _wfreopen((save / L"stdout.log").c_str(), L"w", stdout);
        fs::current_path(base);
        std::vector<std::string> args{utf8(base / L"SonicRidersZeroGravity.exe"), utf8(elf), "--disc", utf8(iso), "--memcard", utf8(card), "--window", "--ntsc"};
        std::vector<char *> argv;
        for (auto &arg : args) argv.push_back(arg.data());
        argv.push_back(nullptr);
        const int result = sonic_main(static_cast<int>(args.size()), argv.data());
        if (result) notice(L"The game could not start. See %LOCALAPPDATA%\\SonicRidersZeroGravity\\game.log for details.");
        return result;
    } catch (const std::exception &error) {
        MessageBoxA(nullptr, error.what(), "Sonic Riders Zero Gravity", MB_OK | MB_ICONERROR);
        return 1;
    }
}
