using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace ALDExplorer2
{
    static class ShiftJisUtil
    {
        static readonly Encoding shiftJis = Encoding.GetEncoding("shift_jis");

        static byte[] validCharacters = null;
        static char invalidChar = Encoding.GetEncoding("shift_jis").GetString(new byte[] { 0xFF, 0xFF })[0];

        private static byte[] GetCharacterLengths()
        {
            byte[] validCharacters = new byte[65536];

            for (int i = 0; i < 65536; i++)
            {
                byte result = 0;
                int b = i & 0xFF;
                int b2 = i >> 8;

                if (b >= ' ' && b <= '~')
                {
                    //ASCII
                    result = 1;
                }
                if (b >= 0xA1 && b <= 0xDF)
                {
                    //halfwidth katakana
                    result = 1;
                }
                //is valid two-byte Shift-Jis sequence
                if ((b >= 0x81 && b <= 0x84) || (b >= 0x87 && b <= 0x9F) || (b >= 0xE0 && b <= 0xEA) || (b >= 0xED && b <= 0xEE) || (b >= 0xFA) && b <= 0xFC)
                {
                    if (b2 >= 0x40 && b2 <= 0xFC && b2 != 0x7F)
                    {
                        var bytes2 = new byte[] { (byte)b, (byte)b2 };
                        string decoded = shiftJis.GetString(bytes2);
                        if (decoded[0] == invalidChar)
                        {
                            result = 0;
                        }
                        else
                        {
                            result = 2;
                        }
                    }
                }
                validCharacters[i] = result;
            }
            return validCharacters;
        }

        public static int GetCharacterLength(IList<byte> bytes, int i)
        {
            if (i >= bytes.Count)
            {
                return 0;
            }
            int b1 = bytes[i];
            int b2 = 0;
            if (i + 1 < bytes.Count)
            {
                b2 = bytes[i + 1];
            }
            if (validCharacters == null)
            {
                validCharacters = GetCharacterLengths();
            }
            return validCharacters[b1 + (b2 << 8)];
        }

        public static int NumberOfValidBytesAtPosition(IList<byte> bytes, int startPosition)
        {
            int count = 0;
            int i = 0;

            while (true)
            {
                int validCount = GetCharacterLength(bytes, i + startPosition);
                if (validCount == 0)
                {
                    break;
                }
                i += validCount;
                count += validCount;
            }
            return count;
        }

        public static int NumberOfValidBytesAtPositionNoAscii(IList<byte> bytes, int startPosition)
        {
            int count = 0;
            int i = 0;

            while (true)
            {
                int validCount = GetCharacterLength(bytes, i + startPosition);
                if (validCount == 1)
                {
                    int c = bytes[i + startPosition];
                    if (c < 0x80 && c != ' ')
                    {
                        break;
                    }
                }
                if (validCount == 0)
                {
                    break;
                }
                i += validCount;
                count += validCount;
            }
            return count;
        }

        public static int NumberOfValidBytesAtPositionNoHalfwidth(IList<byte> bytes, int startPosition)
        {
            int count = 0;
            int i = 0;

            while (true)
            {
                int validCount = GetCharacterLength(bytes, i + startPosition);
                if (validCount == 1)
                {
                    int c = bytes[i + startPosition];
                    if (c > 0x80)
                    {
                        break;
                    }
                }
                if (validCount == 0)
                {
                    break;
                }
                i += validCount;
                count += validCount;
            }
            return count;
        }

        public static int NumberOfValidBytesAtPositionNoSingleByte(IList<byte> bytes, int startPosition)
        {
            int count = 0;
            int i = 0;

            while (true)
            {
                int validCount = GetCharacterLength(bytes, i + startPosition);
                if (validCount == 1)
                {
                    break;
                }
                if (validCount == 0)
                {
                    break;
                }
                i += validCount;
                count += validCount;
            }
            return count;
        }

        static Dictionary<char, char> MakeFullwidthTable = GetMakeFullwidthTable();

        private static Dictionary<char, char> GetMakeFullwidthTable()
        {
            string replace1 = " ｡｢｣､･ｦｧｨｩｪｫｬｭｮｯｰｱｲｳｴｵｶｷｸｹｺｻｼｽｾｿﾀﾁﾂﾃﾄﾅﾆﾇﾈﾉﾊﾋﾌﾍﾎﾏﾐﾑﾒﾓﾔﾕﾖﾗﾘﾙﾚﾛﾜﾝﾞﾟ";
            string replace2 = "　。「」、・をぁぃぅぇぉゃゅょっーあいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわん゛゜";

            if (replace1.Length != replace2.Length)
            {
                throw new ArgumentException("Replacement table is bad");
            }

            Dictionary<char, char> dic = new Dictionary<char, char>();

            for (int i = 0; i < replace1.Length; i++)
            {
                char c1 = replace1[i];
                char c2 = replace2[i];

                dic[c1] = c2;
            }
            return dic;
        }

        public static string HalfWidthKatakanaToHiragana(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (MakeFullwidthTable.ContainsKey(c))
                {
                    sb.Append(MakeFullwidthTable[c]);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
