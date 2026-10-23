using System;
using System.Globalization;
using System.Linq;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionSession
    {
        private void ChangeProject(string command)
        {
            if(pending!=null)throw new InvalidOperationException("Finish the pending save before changing projects.");
            const string add="project:add:",set="project:set:",remove="project:remove:";
            string id;int quantity;
            if(command.StartsWith(add,StringComparison.Ordinal))
            {
                id=command.Substring(add.Length);quantity=1;
                if(snapshot.projects.Any(project=>project.rootId==id)){message="Project already tracked. Its remaining quantity is unchanged.";Render();return;}
            }
            else if(command.StartsWith(remove,StringComparison.Ordinal)){id=command.Substring(remove.Length);quantity=0;}
            else if(command.StartsWith(set,StringComparison.Ordinal))
            {
                var value=command.Substring(set.Length);int separator=value.IndexOf(':');
                if(separator<1||!int.TryParse(value.Substring(0,separator),NumberStyles.None,CultureInfo.InvariantCulture,out quantity)||quantity<1||quantity>99)
                    throw new InvalidOperationException("Choose a project quantity from 1 to 99.");
                id=value.Substring(separator+1);
            }
            else throw new InvalidOperationException("Unknown project action.");
            var existing=snapshot.projects.FirstOrDefault(project=>project.rootId==id);
            if(existing==null)
            {
                if(quantity==0)return;
                if(CampMenuCatalog.Load()?.Tree(id)==null)throw new InvalidOperationException("Choose an available crafting tree to track a new project.");
            }
            if(existing?.remaining==quantity)return;
            message=null;Apply(ledger.PrepareProject(id,quantity));
        }
    }
}
