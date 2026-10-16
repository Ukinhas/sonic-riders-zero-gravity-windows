// Integration checks for the launcher assembly. Run with a disposable profile.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
class ImporterTests {
    sealed class IgnoreProgress : IProgress<int> { public void Report(int n) {} }
    static int count;
    static void Check(bool condition, string name) {
        if (!condition) throw new Exception("FAIL: " + name);
        Console.WriteLine("PASS: " + name); count++;
    }
    static void Main(string[] args) {
        string launcher = Path.GetFullPath(args[0]), iso = Path.GetFullPath(args[1]), root = Path.GetFullPath(args[2]);
        if (Directory.Exists(root)) throw new Exception("Use a new disposable test directory.");
        Directory.CreateDirectory(root);
        var type = Assembly.LoadFrom(launcher).GetType("ImportWindow", true);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var extract = type.GetMethod("ExtractElf", BindingFlags.Static | BindingFlags.NonPublic);
        byte[] elf = (byte[])extract.Invoke(null, new object[] { iso });
        Check(elf.Length == 5998128, "supported ISO extracts the expected ELF");
        string bad = Path.Combine(root,"truncated.iso"); File.WriteAllBytes(bad,new byte[64]);
        try { extract.Invoke(null, new object[] { bad }); throw new Exception("accepted truncated ISO"); }
        catch (TargetInvocationException e) { Check(e.InnerException is InvalidDataException, "truncated ISO rejected"); }
        File.WriteAllBytes(bad,new byte[65536]);
        try { extract.Invoke(null, new object[] { bad }); throw new Exception("accepted invalid ISO"); }
        catch (TargetInvocationException e) { Check(e.InnerException is InvalidDataException, "invalid ISO header rejected"); }
        var instance = FormatterServices.GetUninitializedObject(type);
        type.GetField("saveRoot",flags).SetValue(instance,root);
        var installed = type.GetMethod("InstalledDirectory",flags);
        Check(installed.Invoke(instance,null) == null,"empty profile has no installation");
        File.WriteAllText(Path.Combine(root,"installed.txt"),"install-../../escape");
        Check(installed.Invoke(instance,null) == null,"installation pointer traversal rejected");
        File.WriteAllText(Path.Combine(root,"installed.txt"),"install-"+Guid.NewGuid().ToString("N"));
        Check(installed.Invoke(instance,null) == null,"missing installation rejected");
        using(var cancel=new CancellationTokenSource()) {
            cancel.Cancel();
            try {
                type.GetMethod("Install",flags).Invoke(instance,new object[]{iso,new IgnoreProgress(),cancel.Token});
                throw new Exception("cancel ignored");
            } catch(TargetInvocationException e) { Check(e.InnerException is OperationCanceledException,"import cancellation honored"); }
        }
        Check(Directory.GetDirectories(root,"install-*").Length==0,"cancelled staging data removed");
        Check(!File.Exists(Path.Combine(root,"Mcd001.ps2")),"failed imports do not create or modify saves");
        string atomic=Path.Combine(root,"atomic.txt");
        var write=type.GetMethod("AtomicText",BindingFlags.Static|BindingFlags.NonPublic);
        write.Invoke(null,new object[]{atomic,"one"}); write.Invoke(null,new object[]{atomic,"two"});
        Check(File.ReadAllText(atomic)=="two","atomic settings creation and replacement");
        Console.WriteLine("TOTAL: " + count + " passed");
    }
}
