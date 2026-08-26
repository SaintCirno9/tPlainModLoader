using PixelArt.Utils;
using System;
using tContentPatch.Content.UI;

namespace PixelArt.Content.UI
{
    internal class UITextBoxBind<T> : UITextBox, IBindUIAVal<T>
    {
        public UITextBoxBind(GetSetReset<T> gsr, Func<string, T> parseTry,
            string text_default = "") : base(text_default)
        {
            Action commitAction = () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Text))
                    {
                        SetUIVal(gsr.val);
                        return;
                    }

                    T parseV = parseTry(Text);
                    if (parseV == null && gsr.val == null) return;
                    if (parseV?.Equals(gsr.val) == true) return;
                    OnUIUpdate?.Invoke(parseV);
                }
                catch
                {
                    SetUIVal(gsr.val);
                    return;
                }
            };

            OnLostFocus += commitAction;
            OnSubmit += _ => commitAction();

            BindUIAVal.Bind(gsr, this);
        }

        public event Action<T> OnUIUpdate;

        public void SetUIVal(T v) => Text = v?.ToString();
    }
}
