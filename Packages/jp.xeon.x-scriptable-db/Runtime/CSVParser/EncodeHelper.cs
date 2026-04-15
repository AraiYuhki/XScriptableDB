using System.IO;
using System.Text;

namespace Xeon.XScriptableDB.IO
{
    internal static class EncodeHelper
    {
        /// <summary>
        /// Detects the Japanese character encoding of a file.
        /// <see href="https://qiita.com/nekotadon/items/c1478b5655755018c67c"/>
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="file">File path</param>
        /// <param name="maxSize">Maximum number of bytes to read</param>
        /// <returns></returns>
        internal static Encoding GetJpEncoding(string file, long maxSize = 50 * 1024)// file path, maximum bytes to read
        {
            try
            {
                if (!File.Exists(file)) return null;
                if (new FileInfo(file).Length == 0) return null;

                // Read binary data
                byte[] bytes = null;
                var readAll = false;
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var size = fs.Length;

                    if (size <= maxSize)
                    {
                        bytes = new byte[size];
                        fs.Read(bytes, 0, (int)size);
                        readAll = true;
                    }
                    else
                    {
                        bytes = new byte[maxSize];
                        fs.Read(bytes, 0, (int)maxSize);
                    }
                }
                // Detect encoding
                return GetJpEncoding(bytes, readAll);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Detects the Japanese character encoding (UTF-8, Shift-JIS, EUC-JP, ISO-2022-JP) from a byte array.
        /// Checks for a BOM first, then evaluates each encoding's byte patterns and Japanese-likelihood score.
        /// </summary>
        /// <param name="bytes">Byte array to analyze</param>
        /// <param name="readAll">True if the entire file was read</param>
        /// <returns>The detected encoding, or null if it cannot be determined</returns>
        private static Encoding GetJpEncoding(byte[] bytes, bool readAll = false)
        {
            var len = bytes.Length;

            // BOM detection
            if (len >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff)//UTF-16BE
                return Encoding.BigEndianUnicode;

            if (len >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)//UTF-16LE
                return Encoding.Unicode;

            if (len >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)//UTF-8
                return new UTF8Encoding(true, true);

            if (len >= 3 && bytes[0] == 0x2b && bytes[1] == 0x2f && bytes[2] == 0x76)//UTF-7
                return Encoding.UTF7;

            else if (len >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xfe && bytes[3] == 0xff)//UTF-32BE
                return new UTF32Encoding(true, true);

            else if (len >= 4 && bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0x00 && bytes[3] == 0x00)//UTF-32LE
                return new UTF32Encoding(false, true);

            // Check encoding validity and Japanese-likelihood score simultaneously

            // For Shift_JIS detection
            var sjis = true;         // Whether all bytes fall within the Shift_JIS byte range
            var sjis_2ndbyte = false;// Whether the next byte to check is the second byte of a Shift_JIS multi-byte sequence
            var sjis_kana = false;   // For kana detection
            var sjis_kanji = false;  // For common kanji detection
            var counter_sjis = 0;     // Shift_JIS likelihood score

            // For UTF-8 detection
            var utf8 = true;            // Whether all bytes fall within the UTF-8 byte range
            var utf8_multibyte = false; // Whether the next byte is a continuation byte of a UTF-8 multi-byte sequence
            var utf8_kana_kanji = false;// For kana/common kanji detection
            var counter_utf8 = 0;        // UTF-8 likelihood score
            var counter_utf8_multibyte = 0;

            // For EUC-JP detection
            var eucjp = true;            // Whether all bytes fall within the EUC-JP byte range
            var eucjp_multibyte = false; // Whether the next byte is a continuation byte of an EUC-JP multi-byte sequence
            var eucjp_kana_kanji = false;// For kana/common kanji detection
            var counter_eucjp = 0;        // EUC-JP likelihood score
            var counter_eucjp_multibyte = 0;

            for (var i = 0; i < len; i++)
            {
                var b = bytes[i];

                // Shift_JIS detection
                if (sjis)
                {
                    if (!sjis_2ndbyte)
                    {
                        if (b == 0x0D                   //CR
                            || b == 0x0A                //LF
                            || b == 0x09                //tab
                            || (0x20 <= b && b <= 0x7E))// ASCII character
                        {
                            counter_sjis++;
                        }
                        else if ((0x81 <= b && b <= 0x9F) || (0xE0 <= b && b <= 0xFC))// First byte of a Shift_JIS 2-byte character
                        {
                            // Prepare to check the second byte
                            sjis_2ndbyte = true;

                            if (0x82 <= b && b <= 0x83)// Shift_JIS kana
                            {
                                sjis_kana = true;
                            }
                            else if ((0x88 <= b && b <= 0x9F) || (0xE0 <= b && b <= 0xE3) || b == 0xE6 || b == 0xE7)// Shift_JIS common kanji
                            {
                                sjis_kanji = true;
                            }
                        }
                        else if (0xA1 <= b && b <= 0xDF)// Shift_JIS single-byte character (half-width kana)
                        {
                            ;
                        }
                        else if (0x00 <= b && b <= 0x7F)// ASCII code
                        {
                            ;
                        }
                        else
                        {
                            // Not Shift_JIS
                            counter_sjis = 0;
                            sjis = false;
                        }
                    }
                    else
                    {
                        if ((0x40 <= b && b <= 0x7E) || (0x80 <= b && b <= 0xFC))// Second byte of a Shift_JIS 2-byte character
                        {
                            if (sjis_kana && 0x40 <= b && b <= 0xF1)// Shift_JIS kana
                            {
                                counter_sjis += 2;
                            }
                            else if (sjis_kanji && 0x40 <= b && b <= 0xFC && b != 0x7F)// Shift_JIS common kanji
                            {
                                counter_sjis += 2;
                            }

                            sjis_2ndbyte = sjis_kana = sjis_kanji = false;
                        }
                        else
                        {
                            // Not Shift_JIS
                            counter_sjis = 0;
                            sjis = false;
                        }
                    }
                }

                // UTF-8 detection
                if (utf8)
                {
                    if (!utf8_multibyte)
                    {
                        if (b == 0x0D                   //CR
                            || b == 0x0A                //LF
                            || b == 0x09                //tab
                            || (0x20 <= b && b <= 0x7E))// ASCII character
                        {
                            counter_utf8++;
                        }
                        else if (0xC2 <= b && b <= 0xDF)// 2-byte character
                        {
                            utf8_multibyte = true;
                            counter_utf8_multibyte = 1;
                        }
                        else if (0xE0 <= b && b <= 0xEF)// 3-byte character
                        {
                            utf8_multibyte = true;
                            counter_utf8_multibyte = 2;

                            if (b == 0xE3 || (0xE4 <= b && b <= 0xE9))
                            {
                                utf8_kana_kanji = true;// kana/common kanji
                            }
                        }
                        else if (0xF0 <= b && b <= 0xF3)// 4-byte character
                        {
                            utf8_multibyte = true;
                            counter_utf8_multibyte = 3;
                        }
                        else if (0x00 <= b && b <= 0x7F)// ASCII code
                        {
                            ;
                        }
                        else
                        {
                            // Not UTF-8
                            counter_utf8 = 0;
                            utf8 = false;
                        }
                    }
                    else
                    {
                        if (counter_utf8_multibyte > 0)
                        {
                            counter_utf8_multibyte--;

                            if (b < 0x80 || 0xBF < b)
                            {
                                // Not UTF-8
                                counter_utf8 = 0;
                                utf8 = false;
                            }
                        }

                        if (utf8 && counter_utf8_multibyte == 0)
                        {
                            if (utf8_kana_kanji)
                            {
                                counter_utf8 += 3;
                            }
                            utf8_multibyte = utf8_kana_kanji = false;
                        }
                    }
                }

                // EUC-JP detection
                if (eucjp)
                {
                    if (!eucjp_multibyte)
                    {
                        if (b == 0x0D                   //CR
                            || b == 0x0A                //LF
                            || b == 0x09                //tab
                            || (0x20 <= b && b <= 0x7E))// ASCII character
                        {
                            counter_eucjp++;
                        }
                        else if (b == 0x8E || (0xA1 <= b && b <= 0xA8) || b == 0xAD || (0xB0 <= b && b <= 0xFE))// 2-byte character
                        {
                            eucjp_multibyte = true;
                            counter_eucjp_multibyte = 1;

                            if (b == 0xA4 || b == 0xA5 || (0xB0 <= b && b <= 0xEE))
                            {
                                eucjp_kana_kanji = true;
                            }
                        }
                        else if (b == 0x8F)// 3-byte character
                        {
                            eucjp_multibyte = true;
                            counter_eucjp_multibyte = 2;
                        }
                        else if (0x00 <= b && b <= 0x7F)// ASCII code
                        {
                            ;
                        }
                        else
                        {
                            // Not EUC-JP
                            counter_eucjp = 0;
                            eucjp = false;
                        }
                    }
                    else
                    {
                        if (counter_eucjp_multibyte > 0)
                        {
                            counter_eucjp_multibyte--;

                            if (b < 0xA1 || 0xFE < b)
                            {
                                // Not EUC-JP
                                counter_eucjp = 0;
                                eucjp = false;
                            }
                        }

                        if (eucjp && counter_eucjp_multibyte == 0)
                        {
                            if (eucjp_kana_kanji)
                            {
                                counter_eucjp += 2;
                            }
                            eucjp_multibyte = eucjp_kana_kanji = false;
                        }
                    }
                }

                // ISO-2022-JP detection
                if (b == 0x1B)
                {
                    if ((i + 2 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x40)                                                                           //1B-24-40
                        || (i + 2 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x42)                                                                        //1B-24-42
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x4A)                                                                        //1B-28-4A
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x49)                                                                        //1B-28-49
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x42)                                                                        //1B-28-42
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x44)                                                //1B-24-48-44
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x4F)                                                //1B-24-48-4F
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x51)                                                //1B-24-48-51
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x50)                                                //1B-24-48-50
                        || (i + 5 < len && bytes[i + 1] == 0x26 && bytes[i + 2] == 0x40 && bytes[i + 3] == 0x1B && bytes[i + 4] == 0x24 && bytes[i + 5] == 0x42)//1B-26-40-1B-24-42
                    )
                    {
                        return Encoding.GetEncoding(50220);// iso-2022-jp
                    }
                }
            }

            // If the entire file was read and it ends mid-way through a multi-byte character, detection fails
            if (readAll)
            {
                if (sjis && sjis_2ndbyte)
                {
                    sjis = false;
                }

                if (utf8 && utf8_multibyte)
                {
                    utf8 = false;
                }

                if (eucjp && eucjp_multibyte)
                {
                    eucjp = false;
                }
            }

            if (sjis || utf8 || eucjp)
            {
                // Find the highest Japanese-likelihood score
                int max_value = counter_eucjp;
                if (counter_sjis > max_value)
                {
                    max_value = counter_sjis;
                }
                if (counter_utf8 > max_value)
                {
                    max_value = counter_utf8;
                }

                // Determine the encoding
                if (max_value == counter_utf8)
                    return new UTF8Encoding(false, true);// utf8
                if (max_value == counter_sjis)
                    return Encoding.GetEncoding(932);// ShiftJIS
                else
                    return Encoding.GetEncoding(51932);// EUC-JP
            }
            else
            {
                return null;
            }
        }
    }
}
