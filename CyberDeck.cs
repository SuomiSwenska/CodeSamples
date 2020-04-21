using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CyberDeck : MonoBehaviour
{
    public HardwareShop hardwareShop;
    public static CyberDeck instance;
    public CurrentConfig currentConfig;
    //public HardwareShop hdShop;

    public Motherboard cyberDeck;

    public bool log;

    public float freeRam
    {
        get
        {
            float totalRam = 0;
            for(int i = 0; i < cyberDeck.ram.Count; i++)
            {
                if (cyberDeck.ram[i] != null) totalRam += cyberDeck.ram[i].memorySize;
            }

            if (log) Debug.Log("Free RAM = " + totalRam + " " + usedRam);

            return totalRam - usedRam;
        }
    }

    public float usedRam
    {
        get
        {
            float usedRam = 0;
            for (int i = 0; i < AppsManager.instance.Apps.Count; i++)
            {
                if (log) Debug.Log("App " + AppsManager.instance.Apps[i].name + " in ram " + AppsManager.instance.Apps[i].inRAM + " (" + (36 * AppsManager.instance.Apps[i].ramUsage * AppsManager.instance.Apps[i].loadingProgress) + ")");
                if (!AppsManager.instance.Apps[i].inRAM) continue;
                usedRam += 36 * AppsManager.instance.Apps[i].ramUsage * AppsManager.instance.Apps[i].loadingProgress;
            }

            return usedRam;
        }
    }


    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        LoadHardware();
    }

    public string info;
    private void Update()
    {
        info = "" + freeRam + " " + usedRam;    
    }

    public bool SetHardware(Hardware hardware)
    {
        bool success = false;

        if (hardware is CPU cpu)
        {
            if (cpu.slotType == cyberDeck.cpuSlotType && cpu != cyberDeck.cpu)
            {
                cyberDeck.cpu = cpu;
                success = true;
            }
        }
        else if (hardware is RAM ram)
        {
            if (ram.slotType == cyberDeck.ramSlotType)
            {
                for (int i = 0; i < cyberDeck.ram.Count; i++)
                {
                    if (/*cyberDeck.ram[i].memoryFrequency < ram.memoryFrequency && */cyberDeck.ram[i] != ram)
                    {
                        cyberDeck.ram[i] = ram;
                        success = true;
                        Debug.LogWarning("Success " + success + " Buy: " + hardware.itemName + "// NowDeck: " + cyberDeck.itemName + "|" + cyberDeck.cpu.itemName + "|" + cyberDeck.hdd.itemName);
                        break;
                    }
                }
            }
        }
        else if (hardware is HardDisk hdd)
        {
            if (hdd.slotType == cyberDeck.driveSlotType && hdd != cyberDeck.hdd) { cyberDeck.hdd = hdd; success = true; }
        }
        else if (hardware is Motherboard deck)
        {
            //if (deck.slotType == cyberDeck.driveSlotType) { cyberDeck = deck; success = true; }
            if (cyberDeck != deck) { cyberDeck = deck; success = true; }
        }

        Debug.LogWarning("Success " + success + " Buy: " + hardware.itemName + "// NowDeck: " + cyberDeck.itemName + "|" + cyberDeck.cpu.itemName + "|" + cyberDeck.hdd.itemName);

        if(success) SaveHardware();
        return success;
    }

    public void SaveHardware()
    {
        currentConfig.UpdateCurrentDeckInfo(cyberDeck);

        //HardwareShop hardwareShop = HardwareShop.instance;

        PlayerPrefs.SetInt("Deck", hardwareShop.GetHardwareIndex(cyberDeck));
        PlayerPrefs.SetInt("CPU", hardwareShop.GetHardwareIndex(cyberDeck.cpu));
        PlayerPrefs.SetInt("HDD", hardwareShop.GetHardwareIndex(cyberDeck.hdd));

        PlayerPrefs.SetInt("RAMCount", cyberDeck.ram.Count);

        for (int i = 0; i < cyberDeck.ram.Count; i++)
        {
            PlayerPrefs.SetInt("RAM" + i, hardwareShop.GetHardwareIndex(cyberDeck.ram[i]));
        }

        //Debug.Log("Deck " + hardwareShop.GetHardwareIndex(cyberDeck));
        //Debug.Log("CPU " + hardwareShop.GetHardwareIndex(cyberDeck.cpu));
        //Debug.Log("HDD " + hardwareShop.GetHardwareIndex(cyberDeck.hdd));
        //Debug.Log("RAM " + hardwareShop.GetHardwareIndex(cyberDeck.ram[0]));

        PlayerPrefs.SetInt("HardwareSaved", -1);
    }

    private void LoadHardware()
    {
        currentConfig.UpdateCurrentDeckInfo(cyberDeck);
        if (PlayerPrefs.GetInt("HardwareSaved") != -1) return;

        int motherboardIndex = PlayerPrefs.GetInt("Deck");
        int cpuIndex = PlayerPrefs.GetInt("CPU");
        int hddIndex = PlayerPrefs.GetInt("HDD");

        cyberDeck = (Motherboard)hardwareShop.GetHardwareByIndex(motherboardIndex);
        cyberDeck.cpu = (CPU)hardwareShop.GetHardwareByIndex(cpuIndex);
        cyberDeck.hdd = (HardDisk)hardwareShop.GetHardwareByIndex(hddIndex);

        int ramCount = PlayerPrefs.GetInt("RAMCount");

        for (int i = 0; i < ramCount; i++)
        {
            int ramIndex = PlayerPrefs.GetInt("RAM" + i);
            cyberDeck.ram[i] = (RAM)hardwareShop.GetHardwareByIndex(ramIndex);
        }

        //Debug.LogWarning("Deck " + hardwareShop.GetHardwareIndex(cyberDeck));
        //Debug.LogWarning("CPU " + hardwareShop.GetHardwareIndex(cyberDeck.cpu));
        //Debug.LogWarning("HDD " + hardwareShop.GetHardwareIndex(cyberDeck.hdd));
        //Debug.LogWarning("RAM " + hardwareShop.GetHardwareIndex(cyberDeck.ram[0]));

        currentConfig.UpdateCurrentDeckInfo(cyberDeck);
    }

    //public Hardware GetHardware(string name, int type)
    //{
    //    Hardware hardware = null;

    //    foreach (var item in hardwarePrefs)
    //    {
    //        Debug.Log(item.name);
    //        if (item.name == name) hardware = item;
    //    }

    //    Debug.LogWarning((hardware != null) + " " + name);

    //    hardware.slotType = type;

    //    return hardware;
    //}
}
