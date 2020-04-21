using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppsManager : MonoBehaviour
{
    public static AppsManager instance;

    public Exploiter exploiter;
    public VScanner vscanner;

    public GameObject appsPanelContent;
    public GameObject RAMContent;
    public GameObject appsForSelectContent;

    public GameObject appsPanelCellPref;
    public GameObject RAMCellPref;
    public GameObject appsForSelectPref;

    public GameObject RAM_Cell_Default;

    private List<App> apps = new List<App>();
    public List<App> Apps { get => apps; set => apps = value; }

    private List<GameObject> appsForSelectCells = new List<GameObject>();
    private List<GameObject> appsPanelCells = new List<GameObject>();
    [SerializeField] private List<GameObject> RAMPanelCells = new List<GameObject>();

    private void Awake()
    {
        instance = this;
        FirstStart();
    }

    private void Start()
    {
        LoadApps();
        cnt = apps.Count;
    }

    int cnt = 0;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) FirstStart();
        if (Input.GetKeyDown(KeyCode.M)) Wallet.instance.AddMoney(1000);
        if (Input.GetKeyDown(KeyCode.Z)) Wallet.instance.Test();
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.LogWarning("AppsList " + Apps.Count + " | appsForSelectCells " + appsForSelectCells.Count + " | RAMPanelCells " + RAMPanelCells.Count);
            foreach (var item in Apps)
            {
                Debug.Log(item.name + " | inRAM " + item.inRAM);
            }
        }

        if (Apps.Count != cnt) { Debug.LogError("Apps Count Changed!!!"); cnt = Apps.Count; }
    }

    private int ramSize = 1;
    public void SaveApps()
    {
        PlayerPrefs.SetInt("RAMSize", ramSize);
        PlayerPrefs.SetInt("SavedAppsListCount", Apps.Count);

        Debug.LogWarning("RAMSize " + ramSize + " // SavedAppsListCount " + Apps.Count);

        for (int i = 0; i < Apps.Count; i++)
        {
            string appName = Apps[i].name;
            int version = Apps[i].version;
            int indexInRam = GetAppsIndexInRAM(Apps[i]);
            if (indexInRam < 0) Apps[i].inRAM = false;
            NodeInfoStorage.Instance().SaveApp(appName, version, i, Apps[i].inRAM);

            if(Apps[i].inRAM)
            {
                //Debug.LogError("Save_ " + Apps[i].name + " " + indexInRam);
                PlayerPrefs.SetInt("IndexInRAM" + Apps[i].name, indexInRam);
            }
        }
    }

    public void LoadApps()
    {
        Apps.Clear();

        int RAMSize = PlayerPrefs.GetInt("RAMSize");
        int count = PlayerPrefs.GetInt("SavedAppsListCount");

        Debug.LogError("RAMSize " + RAMSize + " // SavedAppsListCount " + count);

        Host host = new Host(new Vector2Int(0, 0), new Vector2Int(0, 0), null);

        for (int i = 0; i < count; i++)
        {
            KeyValuePair<string, KeyValuePair<int, bool>> kvp = NodeInfoStorage.Instance().GetSavedApp(i);

            App app = AppFactory.instance.CreateApp(kvp.Key, kvp.Value.Key, host);
            app.inRAM = kvp.Value.Value;

            Debug.Log("App " + app.name + " | inRAM " + app.inRAM + " | version " + app.version);

            int indexInRAM = -1;
            if(app.inRAM) indexInRAM = PlayerPrefs.GetInt("IndexInRAM" + app.name, indexInRAM);

            //if (indexInRAM >= 0) InstantiateCell(app, indexInRAM);
            //else app.inRAM = false;

            InstantiateCell(app, indexInRAM);
        }

        GameObject RAMPanel = Instantiate(RAM_Cell_Default, RAMContent.transform);
        RAMPanel.SetActive(true);
        RAMPanelCells.Add(RAMPanel);
    }

    public void AddRAM_Cell()
    {
        //Debug.LogWarning("AddRAM_Cell");
        GameObject RAMPanel = Instantiate(RAMCellPref, RAMContent.transform);
        RAMPanel.SetActive(true);
        RAMPanelCells.Add(RAMPanel);
        ramSize = RAMPanelCells.Count;

        SaveApps();
    }

    public int GetRAM_CellsCount()
    {
        return RAMPanelCells.Count;
    }

    public void RemoveRAM_Cell(GameObject ram_cell)
    {
        for (int i = 0; i < RAMPanelCells.Count; i++)
        {
            if (RAMPanelCells[i] == ram_cell)
            {
                Debug.Log("Remove " + RAMPanelCells[i].name);
                Destroy(RAMPanelCells[i]);
                RAMPanelCells.Remove(RAMPanelCells[i]);
            }
        }

        ramSize = RAMPanelCells.Count;

        SaveApps();
    }

    public bool IsHaveSelectedCell() //TODO ошибка при отсутсвии выделенной ячейки
    {
        bool isHave = false;

        foreach (var item in RAMPanelCells)
        {
            if (!item.GetComponent<AppUI>().selectImage.IsActive()) isHave = true;
        }

        //Debug.Log("Count " + RAMPanelCells.Count + " // isHave= " + isHave);

        return isHave;
    }

    private void InstantiateCell(App app, int indexInRAM = -1)
    {
        Apps.Add(app);

        GameObject appsPanelCell = Instantiate(appsPanelCellPref, appsPanelContent.transform);
        appsPanelCell.SetActive(true);
        appsPanelCell.GetComponent<APContentCell>().SetAppToCell(app);
        appsPanelCells.Add(appsPanelCell);

        GameObject appsForSelectCell = Instantiate(appsForSelectPref, appsForSelectContent.transform);
        appsForSelectCell.SetActive(true);
        appsForSelectCell.GetComponent<AppForSelectCell>().SetApp(app);
        appsForSelectCells.Add(appsForSelectCell);

        if (indexInRAM >= 0 /*&& indexInRAM < RAMPanelCells.Count*/ && app.inRAM)
        {
            //RAMPanelCells[indexInRAM].GetComponent<AppUI>().SetApp(GetAppType(app));
            Debug.Log("AddCell + inRAM " + app.inRAM);
            GameObject ram_Cell = Instantiate(RAMCellPref, RAMContent.transform);
            ram_Cell.SetActive(true);
            ram_Cell.GetComponent<AppUI>().SetApp(app);
            RAMPanelCells.Add(ram_Cell);
        }

        if (app.name == "OnionProxy") MainPanelController.instance.TurnOnProxyIcon();
        if (app.name == "Exploiter")
        {
            exploiter = (Exploiter)app;
            MainPanelController.instance.TurnOnExploiterIcon();
        }

        if (app is VScanner)
        {
            vscanner = app as VScanner;
        }

        //Debug.LogError(app.version);

        //Debug.Log("Inst " + app.name + " // " + Apps.Count);
    }

    public void AddApplication(App app)
    {
        //Debug.LogError(app.version);
        InstantiateCell(app);

        SaveApps();
    }

    public void UpgradeApplication(App app, bool isCrafted = false)
    {
        App _app = GetAppType(app);
        _app.UpgradeVersion(_app.version + 1);
        Debug.LogWarning("Upgr App " + _app.version);
        //if (isCrafted ) Shop.instance.UpgradeProductInShop(app);
        //if (isCrafted ) PanelsController.instance.shopPanel.GetComponent<Shop>().UpgradeProductInShop(app);

        SaveApps();
        UpdateInfo();
    }

    public void DowngradeApplication(App app, bool isCrafted = false)
    {
        App _app = GetAppType(app);
        _app.UpgradeVersion(_app.version - 1);
        Debug.LogWarning("Upgr App " + _app.version);
        //if (isCrafted ) Shop.instance.UpgradeProductInShop(app);
        //if (isCrafted ) PanelsController.instance.shopPanel.GetComponent<Shop>().UpgradeProductInShop(app);

        SaveApps();
        UpdateInfo();
    }

    public int GetAppVersion(App app)
    {
        int level = -1;

        foreach (var item in Apps)
        {
            if(item.name == app.name)
            {
                level = item.version;
            }
        }

        return level;
    }

    public App GetAppType(App app)
    {
        App findApp = null;

        foreach (var item in Apps)
        {
            if (item.name == app.name)
            {
                findApp = item;
            }
        }
        return findApp;
    }

    private void UpdateInfo()
    {
        foreach (var item in appsPanelCells)
        {
            APContentCell cell = item.GetComponent<APContentCell>();
            if (cell == null) return;

            cell.UpdateCellInfo();
        }

        foreach (var item in RAMPanelCells)
        {
            AppUI cell = item.GetComponent<AppUI>();
            if (cell == null || cell.GetApp() == null) continue;

            cell.UpdateInfo();
        }
    }

    public bool ContainedInRAM(App app)
    {
        bool contained = false;

        foreach (var item in Apps)
        {
            if (item.name == app.name) contained = item.inRAM;
        }

        return contained;
    }

    public AppUI GetRAMCellByApp(App app)
    {
        foreach (var item in RAMPanelCells)
        {
            AppUI appui = item.GetComponent<AppUI>();
            //Debug.Log((appui.GetApp() != null) + " // " + (app != null));
            if (appui.GetApp() != null && appui.GetApp().name == app.name) return appui;
        }

        return null;
    }

    private void FirstStart()
    {
        //PlayerPrefs.SetInt("FirstStart", -1);
        if (PlayerPrefs.GetInt("FirstStart") != 1)
        {
            PlayerPrefs.SetInt("FirstStart", 1);

            Host host = new Host(new Vector2Int(0, 0), new Vector2Int(0, 0), null);

            Apps.Add(AppFactory.instance.CreateApp("NodeScanner", 1, host));
            Apps.Add(AppFactory.instance.CreateApp("FirewallBypasser", 1, host));
            Apps.Add(AppFactory.instance.CreateApp("DataSearcher", 1, host));
            //Apps.Add(AppFactory.instance.CreateApp("LogCleaner", 1, host));
            //Apps.Add(AppFactory.instance.CreateApp("Exploiter", 1, host));

            Debug.LogError("FirstStartSaver");

            SaveApps();
        }
    }

    private int GetAppsIndexInRAM(App app)
    {
        for (int i = 0; i < RAMPanelCells.Count; i++)
        {
            if (RAMPanelCells[i].GetComponent<AppUI>().GetApp() == null) continue;
            if (RAMPanelCells[i].GetComponent<AppUI>().GetApp().name == app.name) return i;
        }

        return -1;
    }

    public void UnSellectAppUI(AppUI appui = null)
    {
        foreach (var item in RAMPanelCells)
        {
            if (item != appui) item.GetComponent<AppUI>().selectImage.enabled = true;
        }
    }

    public bool IsHaveOnionProxy()
    {
        bool isHave = false;

        foreach (var item in Apps)
        {
            if (item.name == "OnionProxy") return true;
        }

        return isHave;
    }

    public int GetCountAppsInRAM()
    {
        int counter = 0;

        foreach (var item in RAMPanelCells)
        {
            if (item.GetComponent<AppUI>().GetApp() != null) counter++;
        }

        return counter;
    }

    //private void Clear()
    //{
    //    if (appsCells.Count == 0) return;

    //    foreach (var item in appsCells)
    //    {
    //        Destroy(item);
    //    }
    //}
}
