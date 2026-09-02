using System;
using TPML;
using TPML.Utils;

namespace ChatAi
{
    internal class LoadKey : ModSetting
    {
        public override bool HasUI => false;
        public override string FilePath => "key.txt";
        public override Type DataType => typeof(string);

        public override void Load(object v)
        {
            if (v == null)
            {
                ModFile.SaveFileTry(FilePath, file =>
                {
                    MyJson1.Save(null, file);
                    return true;
                });

                return;
            }

            ChatAI.SetApiKeyTry(v as string);
        }
    }
}
