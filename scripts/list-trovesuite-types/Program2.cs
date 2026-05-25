using System.Reflection;
var asm = Assembly.LoadFrom("/root/.nuget/packages/trovesuite.package/1.0.0/lib/net10.0/Trovesuite.Package.dll");
var auth = asm.GetType("Trovesuite.Package.Auth.IAuthService")!;
foreach (var m in auth.GetMethods())
  Console.WriteLine(m.ToString());
