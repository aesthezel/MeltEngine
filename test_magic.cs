using MagicPhysX;
using System;
public class Test {
    public unsafe void Run() {
        var a = typeof(MagicPhysX.NativeMethods).GetMethods();
        foreach(var m in a) {
            if(m.Name.Contains("FilterData") || m.Name.Contains("getShapes")) {
                Console.WriteLine(m.Name);
            }
        }
    }
}
