﻿using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public struct BlockInfo
{
    public int number; // Block ID
    public int parts; // Parts of the block
    public string hash; // Block Hash code

    /*------Mining Node Log------*/
    public string timeOfCreationS; // Time of the block creation. Example Format: "13:57:02.807"
    public DateTime timeOfCreation; // Converted time of creation
    public float creationElapsedTime; // Creation elapsed time (I believe it's the creation duration)

    public string timeOfSealingS; // Time of the block sealing. Example Format: "13:57:02.807"
    public DateTime timeOfSealing; // Converted time of sealing

    public string timeOfChainReachS; // Time the block reached the chain. Example Format: "13:57:02.807"
    public DateTime timeOfChainReach; // Converted time the block reached the chain
    /*---------------------------*/

    /*------Other Nodes Log------*/
    public List<string> timeOfImportS; // The time the block was imported. Example Format: "13:57:02.807"
    public List<DateTime> timeOfImport; // Converted time of import
    public List<float> importElapsedTime; // Import elapsed time (I believe it's the import process duration, after the node was received)
    public List<float> importDuration; // Time between Sealing and Importing for each node. Value im milisseconds
    /*---------------------------*/

    /*----------Results----------*/
    public float chainReachDuration; // Time between Sealing and the Chain Reach. Value im milisseconds
    public float minImportDuration; // Min value for the importDuration. Value im milisseconds
    public float maxImportDuration; // Max value for the importDuration. Value im milisseconds
    public float meanImportDuration; // Mean value for the importDuration. Value im milisseconds

    /*---------------------------*/
}

public class LogReader : MonoBehaviour {

    [Header("Control")]
    public bool clearListsAfterProcess = true;
    public string logFolder = "ethProj";
    public int minerNodeLogIndex = 1;
    public int logFileStartIndex = 2;
    public int logFileEndIndex = 25;
    public int minValidBlockImportCount = 15;

    [Header("Results")]
    public float importCount;
    public float minImportTime;
    public float maxImportTime;
    public float meanImportTime;
    public float minChainReachTime;
    public float maxChainReachTime;
    public float meanChainReachTime;

    //Control lists
    private List<string> commitNewMiningWorkLines = new List<string>();
    private List<string> sealedNewBlockLines = new List<string>();
    private List<string> minedPotentialBlockLines = new List<string>();
    private List<string> blockReachedCanonicalChainLines = new List<string>();
    private List<string> lines = new List<string>();
    private List<string> chainSegments = new List<string>();
    private List<float> allImportTimes = new List<float>();

    [Header("Data")]
    public List<int> blocksPerImportCount = new List<int>();
    public List<float> averageTimePerBlockRange = new List<float>();
    public List<BlockInfo> blocks = new List<BlockInfo>();

    void Start()
    {
        for (int i = 0; i < 25; i++)
            blocksPerImportCount.Add(0);

        ParseMinerLog();
        CreateBlocks();
        ParseFiles();

        ClearIncompleteBlocks();
        ProcessBlocks();
        
        if (clearListsAfterProcess)
            ClearLists();
    }

    private void ProcessBlocks()
    {
        BlockInfo __block;
        float __overallImportCounter = 0f;
        float __overallBlockImportInstancesCounter = 0f;
        float __overallChainReachCounter = 0f;
        float __blockImportCounter;

        minImportTime = 100000000f;
        maxImportTime = 0f;
        meanImportTime = 0f;
        minChainReachTime = 1000000f;
        maxChainReachTime = 0f;
        meanChainReachTime = 0f;

        for (int i = 0; i < blocks.Count; i++)
        {
            __block = blocks[i];
            __blockImportCounter = 0f;
            __block.minImportDuration = 1000000f;
            __block.maxImportDuration = 0f;

            for (int j = 0; j < __block.importDuration.Count; j++)
            {
                __blockImportCounter += __block.importDuration[j];
                // Per Block
                if (__block.importDuration[j] > __block.maxImportDuration)
                    __block.maxImportDuration = __block.importDuration[j];
                if (__block.importDuration[j] < __block.minImportDuration)
                    __block.minImportDuration = __block.importDuration[j];
                // Overall Import
                if (__block.importDuration[j] > maxImportTime)
                    maxImportTime = __block.importDuration[j];
                if (__block.importDuration[j] < minImportTime && __block.importDuration[j] >= 10f)
                    minImportTime = __block.importDuration[j];
                
            }
            __block.meanImportDuration = __blockImportCounter / __block.importDuration.Count;
            __overallImportCounter += __blockImportCounter;
            __overallBlockImportInstancesCounter += __block.importDuration.Count;
            __overallChainReachCounter += __block.chainReachDuration;

            // Overall Chain Reach Min-Max
            if (__block.chainReachDuration > maxChainReachTime)
                maxChainReachTime = __block.chainReachDuration;
            if (__block.chainReachDuration < minChainReachTime)
                minChainReachTime = __block.chainReachDuration;
            blocks[i] = __block;
            blocksPerImportCount[__block.importDuration.Count]++;
        }
        importCount = __overallImportCounter;
        meanImportTime =  __overallImportCounter / __overallBlockImportInstancesCounter;
        meanChainReachTime = __overallChainReachCounter / blocks.Count;


        // Average time ber block range
        for(int i = 0; i < blocks.Count; i++)
        {
            int __counter = 0;
            averageTimePerBlockRange.Add(0);
            for (int j = i; j < i + 250; j++)
            {
                if (j == blocks.Count)
                    break;
                __counter++;
                float __sum = 0;
                for (int k = 0; k < blocks[j].importDuration.Count; k++)
                    __sum += blocks[j].importDuration[k];
                averageTimePerBlockRange[averageTimePerBlockRange.Count - 1] += __sum / blocks[j].importDuration.Count;
            }
            averageTimePerBlockRange[averageTimePerBlockRange.Count - 1] /= __counter;
            i += 250;
        }
        
    }

    private void ClearLists()
    {
        commitNewMiningWorkLines.Clear();
        sealedNewBlockLines.Clear();
        minedPotentialBlockLines.Clear();
        blockReachedCanonicalChainLines.Clear();
        lines.Clear();
        chainSegments.Clear();
        
        for (int i = 0; i < blocks.Count; i++)
        {
            blocks[i].timeOfImportS.Clear();
        }
    }

    private void ParseMinerLog()
    {
        TextAsset bindata = Resources.Load(logFolder + "/no" + minerNodeLogIndex.ToString() + "/etherLog") as TextAsset;
        lines = bindata.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        chainSegments = new List<string>(lines);

        for (int i = 0; i < chainSegments.Count; i++)
        //for (int i = 0; i < 1000; i++)
        {
            if (chainSegments[i].Contains("Commit new mining work"))
            {
                commitNewMiningWorkLines.Add(chainSegments[i]);
            }
            else if (chainSegments[i].Contains("Successfully sealed new block"))
            {
                sealedNewBlockLines.Add(chainSegments[i]);
            }
            /*else if (chainSegments[i].Contains("mined potential block"))
            {
                minedPotentialBlockLines.Add(chainSegments[i]);
            }*/
            else if (chainSegments[i].Contains("block reached canonical chain"))
            {
                blockReachedCanonicalChainLines.Add(chainSegments[i]);
            }
        }
        chainSegments.Clear();
    }

    private void CreateBlocks()
    {
        List<string> lineBits = new List<string>();
        string aux;

        /*
         * Creation of new blocks
         * 
         * Log example:
         * INFO [09-12|13:57:02.807] Commit new mining work number=1 txs=0 uncles=0 elapsed=138.511µs
         */
        for (int i = 0; i < commitNewMiningWorkLines.Count; i++)
        {
            lineBits = commitNewMiningWorkLines[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            BlockInfo block = new BlockInfo();

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    aux = aux.Replace("]", "");
                    block.timeOfCreationS = aux;
                    block.timeOfCreation = StringToDateTime(aux);
                }
                else if (bit.StartsWith("number="))
                {
                    block.number = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("elapsed="))
                {
                    aux = bit.Replace("elapsed=", "");
                    aux = aux.Remove(aux.Length - 2);
                    if (bit.Contains('m'))
                        block.creationElapsedTime = float.Parse(aux);
                    else
                        block.creationElapsedTime = float.Parse(aux) / 1000f;
                }
                
            }
            blocks.Add(block);
        }
        /*
          * Sealing process of blocks
          * 
          * Log example:
          * INFO [09-12|13:57:04.249] Successfully sealed new block number=1 hash=38784d…5bb84d
          */
        for (int i = 0; i < sealedNewBlockLines.Count; i++)
        {
            lineBits = sealedNewBlockLines[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            int __blockNumber = 0;
            string __timeOfSealingS = "";
            DateTime __timeOfSealing = new DateTime();
            string __hash = "";

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    aux = aux.Replace("]", "");
                    __timeOfSealingS = aux;
                    __timeOfSealing = StringToDateTime(aux);
                    
                }
                else if (bit.StartsWith("number="))
                {
                    __blockNumber = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("hash="))
                {
                    __hash = bit.Replace("hash=", "");
                }
            }
            for(int j = 0; j< blocks.Count; j ++)
            {
                if (blocks[j].number == __blockNumber)
                {
                    BlockInfo __block = blocks[j];
                    __block.timeOfSealingS = __timeOfSealingS;
                    __block.timeOfSealing = __timeOfSealing;
                    __block.hash = __hash;
                    blocks[j] = __block;
                    break;
                }
            }
           
        }
        /*
         * Block reached the chain
         * 
         * Log example:
         * INFO [09-12|13:57:38.488] 🔗 block reached canonical chain number=26 hash=0e9e8c…2cc91c
         */
        for (int i = 0; i < blockReachedCanonicalChainLines.Count; i++)
        {
            lineBits = blockReachedCanonicalChainLines[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            int __blockNumber = 0;
            string __timeOfChainReachS = "";
            DateTime __timeOfChainReach = new DateTime();
            TimeSpan __ts = new TimeSpan();

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    aux = aux.Replace("]", "");
                    __timeOfChainReachS = aux;
                    __timeOfChainReach = StringToDateTime(aux);
                }
                else if (bit.StartsWith("number="))
                {
                    __blockNumber = int.Parse(bit.Replace("number=", ""));
                }
            }

            for (int j = 0; j < blocks.Count; j++)
            {
                if (blocks[j].number == __blockNumber)
                {
                    BlockInfo __block = blocks[j];
                    __block.timeOfChainReachS = __timeOfChainReachS;
                    __block.timeOfChainReach = StringToDateTime(__timeOfChainReachS);
                    if (__timeOfChainReachS != "")
                    {
                        __ts = __timeOfChainReach - __block.timeOfCreation;
                        __block.chainReachDuration = (__ts.Seconds * 1000) + __ts.Milliseconds;
                    }
                    blocks[j] = __block;
                    break;
                }
            }
        }
    }

    private void ParseFiles()
    {
        for (int i = logFileStartIndex; i <= logFileEndIndex; i++)
            if (i != minerNodeLogIndex)
                ParseFile(i);
    }

    private void ParseFile(int fileIndex)
    {
        /*
         * Imported new chain segment
         * 
         * Log example:
         * INFO [09-12|13:57:10.714] Imported new chain segment 
         *  blocks=2 txs=0 mgas=0.000 elapsed=901.128µs mgasps=0.000 
         *  number=2 hash=2432b5…30bf5e cache=424.00B
         */
        TextAsset bindata = Resources.Load("ethProj/no" + fileIndex.ToString() + "/etherLog") as TextAsset;
        lines = bindata.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
        chainSegments = new List<string>(lines);

        for (int i = chainSegments.Count - 1; i >= 0; i--)
        {
            if (!chainSegments[i].Contains("Imported new chain segment"))
            {
                chainSegments.RemoveAt(i);
            }
        }

        for (int i = 0; i < chainSegments.Count; i++)
        {
            List<string> lineBits = chainSegments[i].Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            string aux;
            int __blockNumber = 0;
            string __timeOfImport = "";
            float __importElapsedTime = 0f;
            /*float __cache = 0f;
            string __hash = "";*/
            int __parts = 0;

            foreach (string bit in lineBits)
            {
                if (bit.StartsWith("blocks="))
                {
                    __parts = int.Parse(bit.Replace("blocks=", ""));
                }
                else if (bit.StartsWith("elapsed="))
                {
                    aux = bit.Replace("elapsed=", "");
                    aux = aux.Remove(aux.Length - 2);
                    if (bit.Contains('m'))
                        __importElapsedTime = float.Parse(aux);
                    else
                        __importElapsedTime = float.Parse(aux) / 1000f;
                }
                else if (bit.StartsWith("number="))
                {
                    __blockNumber = int.Parse(bit.Replace("number=", ""));
                }
                else if (bit.StartsWith("[") && bit.EndsWith("]"))
                {
                    aux = bit.Split('|')[1];
                    __timeOfImport = aux.Replace("]", "");
                }
                /*else if (bit.StartsWith("hash="))
                {
                    __hash = bit.Replace("hash=", ""); ;
                }*/
                /*else if (bit.StartsWith("cache="))
                {
                    aux = bit.Replace("cache=", "");
                    aux = aux.Remove(aux.Length - 2);
                    __cache = float.Parse(aux);
                }*/
            }
            for (int j = 0; j < blocks.Count; j++)
            {
                if (blocks[j].number == __blockNumber)
                {
                    BlockInfo __block = blocks[j];
                    DateTime __dt = new DateTime();
                    TimeSpan __ts = new TimeSpan();
                    if (__block.timeOfImportS == null)
                    {
                        __block.timeOfImportS = new List<string>();
                        __block.timeOfImport = new List<DateTime>();
                        __block.importDuration = new List<float>();
                    }
                    __block.timeOfImportS.Add(__timeOfImport);
                    __dt = StringToDateTime(__timeOfImport);
                    __block.timeOfImport.Add(__dt);
                    __ts = (__dt - __block.timeOfCreation);
                    __block.importDuration.Add((__ts.Seconds * 1000) + __ts.Milliseconds);

                    if (__block.importElapsedTime == null)
                        __block.importElapsedTime = new List<float>();
                    __block.importElapsedTime.Add(__importElapsedTime);
                    __block.parts = __parts;
                    blocks[j] = __block;
                    break;
                }
            }
        }
    }

    private void ClearIncompleteBlocks()
    {
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            if (blocks[i].number == 0 || blocks[i].hash == null || blocks[i].timeOfCreationS == null
                || blocks[i].timeOfSealingS == null || blocks[i].timeOfChainReachS == null || blocks[i].timeOfImportS == null
                || blocks[i].timeOfImportS.Count < minValidBlockImportCount)
                blocks.RemoveAt(i);
        }
    }

    private DateTime StringToDateTime(string text)
    {
        return new DateTime(2018, 9, 12, int.Parse(text.Split(':')[0]), int.Parse(text.Split(':')[1]), 
            int.Parse((text.Split(':')[2]).Split('.')[0]), int.Parse((text.Split(':')[2]).Split('.')[1]));
    }
}
