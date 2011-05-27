using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace oggextract
{
    class Program
    {
        public static BinaryWriter writer;
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("usage : oggextract file");
                return;
            }
 
            string path = args[0];
            BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open), Encoding.ASCII);
            Directory.CreateDirectory(@"output");
            int count = 0;
            while (true)
            {
                try
                {
                    long page_position = reader.BaseStream.Position;
                    if (reader.ReadByte() == 0x4f)
                    {
                        if (new string(reader.ReadChars(3)) == "ggS")
                        {
                            // most likely a page
                            if (reader.ReadByte() != 0x00) // version
                            {
                                reader.BaseStream.Position = page_position + 1;
                                continue;
                            } 
                            int header_type = reader.ReadByte(); //  header type
                            reader.ReadInt64(); // granule position
                            reader.ReadInt32(); // bitstream serial number
                            reader.ReadInt32(); // page sequence number
                            reader.ReadUInt32(); // checksum
                            int page_segments = reader.ReadByte(); // page segments

                            int header_size = page_segments + 27;
                            int page_size = 0;

                            for (int i = 0; i < page_segments; i++)
                            {
                                page_size += reader.ReadByte();
                            }

                            page_size += header_size;
                            reader.BaseStream.Position = page_position;
                            byte[] page = reader.ReadBytes(page_size);

                            if (header_type == 2) // new page
                            {
                                writer = new BinaryWriter(new FileStream(@"output\0x" + page_position.ToString("X") + ".ogg", FileMode.Create), Encoding.ASCII);
                                writer.Write(page);
                            }
                            else if (header_type == 4 || header_type == 5) // end of page
                            {
                                Console.WriteLine("output/0x" + page_position.ToString("X") + ".ogg");
                                writer.Write(page);
                                writer.Flush();
                                writer.Close();
                                count++;
                            }
                            else // continuation of last page
                            {
                                writer.Write(page);
                            }
                        }
                        else
                        {
                            reader.BaseStream.Position = page_position + 1;
                        }
                    }
                }
                catch (EndOfStreamException e)
                {
                    reader.Close();
                    Console.WriteLine("Done! " + count + " files extracted.");
                    break;
                }
            }
        }
    }
}
