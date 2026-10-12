var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
var fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
var assembly=typeof(UnityEditor.Editor).Assembly;
var logs=assembly.GetType("UnityEditor.LogEntries");
var entryType=assembly.GetType("UnityEditor.LogEntry");
var get=logs.GetMethod("GetEntryInternal",flags);
var errors=new System.Collections.Generic.List<object>();
int count=(int)logs.GetMethod("StartGettingEntries",flags).Invoke(null,null);
try
{
    for(int i=0;i<count;i++)
    {
        var entry=System.Activator.CreateInstance(entryType,true);
        if(!(bool)get.Invoke(null,new object[]{i,entry})) throw new System.InvalidOperationException("Unreadable console entry");
        int mode=(int)entryType.GetField("mode",fields).GetValue(entry);
        if((mode & (1|2|16|64|256|2048|131072|1048576|2097152))!=0)
            errors.Add(new { mode=mode,message=(string)entryType.GetField("message",fields).GetValue(entry) });
    }
}
finally {logs.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
return new { entries=count,errors=errors.ToArray() };
