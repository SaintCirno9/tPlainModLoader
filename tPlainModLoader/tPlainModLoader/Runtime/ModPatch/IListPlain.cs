namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 补丁列表统一接口（向后兼容存根）。
    /// 作者: SaintCirno9
    /// </summary>
    public interface IListPlain
    {
        void Clear();
        void AddRange(object list);
    }
}
