namespace FargoItems.Content.Logic
{
    /// <summary>
    /// 防止一次按键保持期间重复启动直通车。
    /// </summary>
    public sealed class InstavatorUseGate
    {
        public bool IsLocked { get; private set; }

        public bool TryAcquire()
        {
            if (IsLocked)
            {
                return false;
            }

            IsLocked = true;
            return true;
        }

        public void Update(bool controlUseItem)
        {
            if (!controlUseItem)
            {
                IsLocked = false;
            }
        }

        public void Reset()
        {
            IsLocked = false;
        }
    }
}
