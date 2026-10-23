using System;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Authored empty layout only. It owns no inventory slots, contents or carrying capacity.</summary>
    public static class CampMenuGarmentPockets
    {
        public const int PageSize=8;
        public const long MaximumAuthoredCount=9007199254740991L;
        public static long Count(CampMenuCatalogItem item,CampMenuStackView owned,string target)
            =>item!=null&&owned!=null&&owned.owner=="booter"&&owned.itemId==item.id&&owned.equipmentAddress==target&&target is "chest" or "legs"&&item.pocketSlot==target&&item.category is "Armor" or "Clothing"&&item.pockets>0&&item.pockets<=MaximumAuthoredCount?item.pockets:0;
        public static string Identity(CampMenuStackView owned)=>owned==null?null:owned.itemId+"|"+(!string.IsNullOrEmpty(owned.instanceId)?"instance:"+owned.instanceId:"location:"+owned.key+"@"+owned.storeRevision);
        public static long Pages(long count)=>count<=0?1:1+(count-1)/PageSize;
        public static long ClampPage(long count,long page)=>Math.Clamp(page,0,Pages(count)-1);
        public static long[] Numbers(long count,long page)
        {
            if(count<=0||count>MaximumAuthoredCount)return Array.Empty<long>();long first=ClampPage(count,page)*PageSize+1;int size=(int)Math.Min(PageSize,count-first+1);var result=new long[size];for(int i=0;i<size;i++)result[i]=first+i;return result;
        }
    }
}
