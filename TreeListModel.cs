using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackCheckTool
{
    public interface ITreeNodeRelationEntity
    {
        int OrderID { get; set; }
        string ID { get; set; }
        string Name { get; set; }
        IEnumerable<string> ParentIDs { get; set; }
    }

    public class TreeNodeRelationEntityBase<T> : ITreeNodeRelationEntity
    {
        public int OrderID { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> ParentIDs { get; set; }
        public T OTag { get; set; }
    }

    public static class TreeListModelConvert
    {
        public static TreeListModel<Entity> ToTreeList<Entity>(this IEnumerable<Entity> v, Func<Entity, bool>? filter = null) where Entity : class, ITreeNodeRelationEntity
        {
            var root = new TreeListModel<Entity>();
            var tlist = new Dictionary<string, TreeListModel<Entity>>();
            foreach (var item in v)
            {
                if (item is Entity t)
                {
                    if (filter == null)
                        tlist[t.ID] = new TreeListModel<Entity>(v, t);
                    else if (filter.Invoke(t))
                        tlist[t.ID] = new TreeListModel<Entity>(v, t);
                }
            }
            var allList = tlist.Values.ToList();
            root.AllTreeList = allList;
            foreach (var item in allList)
            {
                item.AllTreeList = allList;
                if (filter != null)
                {
                    if (!filter.Invoke(item.EntityData))
                        continue;
                }
                var parentID = item.EntityData.ParentIDs.LastOrDefault();
                if (string.IsNullOrEmpty(parentID))
                    root.AddChildNode(item);
                else if (tlist.ContainsKey(parentID))
                    tlist[parentID].AddChildNode(item);
            }

            return root;
        }
    }

    public class TreeListModel<Entity> where Entity : class, ITreeNodeRelationEntity
    {
        public IEnumerable<TreeListModel<Entity>> AllTreeList { get; set; }

        public IEnumerable<Entity> EntityDataList { get; set; }

        public Entity EntityData { get; set; }

        public TreeListModel<Entity> ParentNode { get; set; }

        public List<TreeListModel<Entity>> ChildNodes { get; set; } = new List<TreeListModel<Entity>>();

        public TreeListModel()
        {
        }

        public TreeListModel(Entity entityData)
        {
            EntityData = entityData;
        }

        public TreeListModel(IEnumerable<Entity> entityDataList)
        {
            EntityDataList = entityDataList;
        }

        public TreeListModel(IEnumerable<Entity> entityDataList, Entity entityData)
        {
            EntityData = entityData;
            EntityDataList = entityDataList;
        }

        public void AddChildNode(TreeListModel<Entity> node)
        {
            ChildNodes.Add(node);
            node.ParentNode = this;
        }

        public int RefreshOrderID(int startIndex)
        {
            var tstartIndex = startIndex;
            if (EntityData != null)
            {
                EntityData.OrderID = tstartIndex;
                tstartIndex++;
            }
            if (ChildNodes == null)
                return tstartIndex;
            foreach (var childNode in ChildNodes)
            {
                tstartIndex = childNode.RefreshOrderID(tstartIndex);
            }
            return tstartIndex;
        }
    }
}
