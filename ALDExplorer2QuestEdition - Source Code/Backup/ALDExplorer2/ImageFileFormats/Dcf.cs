using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FreeImageAPI;

namespace ALDExplorer2.ImageFileFormats
{
    public static class Dcf
    {
        public static FreeImageBitmap LoadImage(byte[] bytes)
        {
            //temporary code: Just find the QNT inside and display that instead
            int qntIndex = bytes.IndexOf("QNT", 0);
            bool found = false;
            while (!found && qntIndex >= 0 && qntIndex < bytes.Length)
            {
                if (qntIndex >= 4)
                {
                    int wordBeforeQnt = BitConverter.ToInt32(bytes, qntIndex - 4);
                    if (wordBeforeQnt + qntIndex == bytes.Length)
                    {
                        found = true;
                        break;
                    }
                }
                qntIndex = bytes.IndexOf("QNT", qntIndex + 1);
            }
            if (found)
            {
                bytes = bytes.Skip(qntIndex).ToArray();
                return Qnt.LoadImage(bytes);
            }
            return null;
        }
    }
}
