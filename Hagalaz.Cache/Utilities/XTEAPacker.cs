using System;
using System.IO;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// Provides services for packing map data.
    /// </summary>
    public static class XteaPacker
    {
        /// <summary>
        /// Packs mapdata and stores in a file 
        /// </summary>
        public static void Pack()
        {
            //JEnvironment.Logger.PrintInfo("Packing xtea keys...");
            try
            {
                File.Delete(@"./data/xtea/packed.bin");
            }
            catch (Exception)
            {
            }
            int foundMapdata = 0;
            try
            {
                FileStream fs = File.OpenWrite(@"./data/xtea/packed.bin");
                BinaryWriter writer = new BinaryWriter(fs);
                for (int i = -40000; i < 40000; i++)
                {
                    if (File.Exists(@"../data/xtea/unpacked/" + i + ".txt"))
                    {
                        foundMapdata++;
                        using (StreamReader r = new StreamReader(File.OpenRead(@"./data/xtea/unpacked/" + i + ".txt")))
                        {
                            writer.Write(i); // region
                            for (int x = 0; x < 4; x++)
                                writer.Write(int.Parse(r.ReadLine()!));
                        }
                    }
                }
                writer.Flush();
                writer.Dispose();
            } catch (Exception)
            {

            }
           // JEnvironment.Logger.PrintInfo("Packed " + FoundMapdata + " xtea keys.");
        }
    }
}