using System;
using System.Collections.Generic;
using System.IO;

namespace Hagalaz.Cache.Utilities
{
    /// <summary>
    /// Provides services for loading map data.
    /// </summary>
    public static class XteaLoader
    {
        /// <summary>
        /// Loads map data and stores them in a specified 
        /// </summary>
        /// <param name="regionsCollection">The collection to add region data to.</param>
        public static void Load(Dictionary<int, int[]> regionsCollection)
        {
            if (!File.Exists(@"./data/xtea/packed.bin"))
            {
                XteaPacker.Pack();
            }
            try
            {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(@"./data/xtea/packed.bin")))
                {
                    int filelen = (int)reader.BaseStream.Length;
                    int curr = 0;
                    while (curr < filelen)
                    {
                        int mapId = reader.ReadInt32(); // The map id.
                        int[] mapData = new int[4]; // The map data.

                        for (int i = 0; i < 4; i++)
                            mapData[i] = reader.ReadInt32();

                        regionsCollection.Add(mapId, mapData); // Add the data to collection.
                        curr += 20;
                    }
                }
            }
            catch (Exception)
            {

            }
            //JEnvironment.Logger.PrintInfo("Loaded " + regionsCollection.Count + " xtea keys!");

        }
    }
}

