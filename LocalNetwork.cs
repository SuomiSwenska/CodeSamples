using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System;

public class LocalNetwork : NetworkGenerator
{
    public static LocalNetwork instance; //TODO
    private void Awake()
    {
        instance = this;
    }

    int shiftx = 0;
    int shifty = 0;
    public int height = 5;
    public int width = 5;

    private int[,] net;
    private int[,] roles;
    private NetworkNodeInfo[,] nodes;

    private Link[,] links;

    private int difficulty;

    private class Link
    {
        public int nodeType;
        public List<int> directions;
        public bool closed = false;
        public Vector2 position;

        public Link(List<int> directions, Vector2 pos)
        {
            this.directions = directions;
            if (directions.Count == 0) closed = true;

            this.position = pos;
            //Debug.Log("Link " + pos.x + "-" + pos.y + " route " + directions.Count);
        }

        public void RemoveLink(int dir)
        {
            if (directions.Contains(dir)) directions.Remove(dir);
            CheckClosed();
        }

        public void AddLink(int dir)
        {
            if (!directions.Contains(dir)) directions.Add(dir);
            CheckClosed();
        }

        private void CheckClosed()
        {
            if (directions.Count <= 0) closed = true;
            else closed = false;
        }

        public int GetX()
        {
            return (int)position.x;
        }

        public int GetY()
        {
            return (int)position.y;
        }
    }

    private class TreeNode
    {
        public Vector2Int coord;
        public List<TreeNode> links = new List<TreeNode>();
        public List<int> weights = new List<int>();
        public TreeNode parent;
    }

    private bool analyzedNode = false;
    private bool haveEntryPoint = false;
    private Vector2 entryPoint = Vector2.zero;
    public Vector2Int globalAddress { get; private set; }

    public void Init (GlobalNodeInfo info)
    {
        Vector2Int minCoord = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int maxCoord = new Vector2Int(int.MinValue, int.MinValue);
        for (int i = 0; i < info.localNetwork.Count; i++)
        {   
            for(int j = 0; j < info.localNetwork[i].list.Count; j++)
            {
                minCoord = Vector2Int.Min(minCoord, info.localNetwork[i].list[j].localAddress);
                maxCoord = Vector2Int.Max(maxCoord, info.localNetwork[i].list[j].localAddress);
            }
        }

        Debug.Log("Found " + minCoord + " " + maxCoord);

        difficulty = info.info.nodeDifficulty;
        globalAddress = info.info.globalAddress;
        int x = globalAddress.x;
        int y = globalAddress.y;
        width = maxCoord.x + 1;
        height = maxCoord.y + 1;

        net = new int[width, height];
        roles = new int[width, height];
        nodes = new NetworkNodeInfo[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int nodeType = 0;
                net[i, j] = nodeType;
                roles[i, j] = -1;
            }
        }

        for (int i = 0; i < info.localNetwork.Count; i++)
        {
            for (int j = 0; j < info.localNetwork[i].list.Count; j++)
            {
                var node = info.localNetwork[i].list[j];
                net[node.localAddress.x, node.localAddress.y] = node.nodeType;
                roles[node.localAddress.x, node.localAddress.y] = node.nodeRole;
                nodes[node.localAddress.x, node.localAddress.y] = node;
            }
        }

        entryPoint = info.entryPoint;
    }

    public override NetworkNodeInfo GetNodeInfo(int x, int y)
    {
        //Debug.Log("Get node info " + x + " " + y + " " + nodes.GetLength(0) + " " + nodes.GetLength(1));

        NetworkNodeInfo info = new NetworkNodeInfo();

        if (nodes != null && x >= 0 && x < width && y >= 0 && y < height && nodes[x, y] != null)
        {
            info = nodes[x, y];

            if (info.nodeRole >= 0 && info.nodeRole < Library.instance.rolesSpr.Count) info.sprite = Library.instance.rolesSpr[info.nodeRole];

            return info;
        }

        info.nodeType = GetNodeType(x, y);
        info.nodeRole = GetNodeRole(x, y);
        info.localAddress = new Vector2Int(x, y);
        info.globalAddress = GetGlobalAddress(info.localAddress);
        info.nodeDifficulty = GetNodeDifficulty(x, y);

        int appLevel = GMARandom.Range2D(info.nodeDifficulty * 10, (info.nodeDifficulty + 1) * 10, info.globalAddress.x, info.globalAddress.y);
        if (appLevel < 1) appLevel = 1;

        NetworkNodeInfo.AppSettings app = new NetworkNodeInfo.AppSettings();
        app.appName = "LocalNode";
        app.appLevel = appLevel;
        info.apps.Add(app);

        int xx = info.globalAddress.x + x;
        int yy = info.globalAddress.y + y;

        appLevel = GMARandom.Range2D(appLevel - 1, appLevel + 2, xx, yy);
        if (appLevel == 0) appLevel = 1;


        if (info.nodeRole == 1)
        {
            app = new NetworkNodeInfo.AppSettings();
            app.appName = "SecurityModule";
            app.appLevel = appLevel;
            info.apps.Add(app);
        }
        else if (info.nodeRole == 4)
        {
            app = new NetworkNodeInfo.AppSettings();
            app.appName = "DataStorage";
            app.appLevel = appLevel;
            info.apps.Add(app);
        }
        else if (info.nodeRole == 5)
        {
            app = new NetworkNodeInfo.AppSettings();
            app.appName = "PersonalComputer";
            app.appLevel = appLevel;
            info.apps.Add(app);
        }

        if (info.nodeRole >= 0 && info.nodeRole < Library.instance.rolesSpr.Count) info.sprite = Library.instance.rolesSpr[info.nodeRole];

        return info;
    }

    public override void Init (int x, int y, int difficulty)
    {
        globalAddress = new Vector2Int(x, y);

        GlobalNodeInfo info = null;
        if (GameController.instance._globalNodesOverrides.ContainsKey(globalAddress)) 
            info = GameController.instance._globalNodesOverrides[globalAddress];

        if (info != null)
        {
            Init(info);
            return;
        }
        else nodes = null;   

        width = 2 + GMARandom.Range3D(difficulty, (1 + difficulty) * 2, x, y, 1);
        height = 2 + GMARandom.Range3D(difficulty, (1 + difficulty) * 2, x, y, 2);

        this.difficulty = difficulty;

        //width = 2;
        //height = 1;

        Debug.Log("Init " + x + " " + y + " " + difficulty + " " + width + " " + height);

        net = new int[width, height];
        links = new Link[width, height];
        roles = new int[width, height];

        haveEntryPoint = false;
        shiftx = GMARandom.Range2D(-1000000, 100000, x + y, (x + 1) * (y + 1));
        shifty = GMARandom.Range2D(-1000000, 100000, x - y, x + y);
        //shiftx = -863620;
        //shifty = 52045;

        Debug.Log(x + " " + y + " " + shiftx + "  " + shifty);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int nodeType = Node.GetType(i + shiftx, j + shifty);
                net[i, j] = nodeType;

                links[i, j] = GetLink(nodeType, new Vector2(i, j));
                links[i, j].nodeType = nodeType;

                roles[i, j] = -1;
            }
        }

        for (int i = 0; i < width; i++)
        {
            net[i, 0] = Node.RemoveLink(net[i, 0], 2);
            links[i, 0].RemoveLink(2);

            //if ((net[i, 0] == 1 || net[i, 0] == 8 || net[i, 0] == 2) && !haveEntryPoint) //TODO entryPoint
            //{
            //    entryPoint.x = i;
            //    entryPoint.y = 0;
            //    haveEntryPoint = true;
            //}

            net[i, height - 1] = Node.RemoveLink(net[i, height - 1], 0);
            links[i, height - 1].RemoveLink(0);
        }

        for (int j = 0; j < height; j++)
        {
            net[0, j] = Node.RemoveLink(net[0, j], 3);
            links[0, j].RemoveLink(3);

            //if ((net[0, j] == 1 || net[0, j] == 2) && !haveEntryPoint)
            //{
            //    entryPoint.x = 0;
            //    entryPoint.y = j;
            //    haveEntryPoint = true;
            //}

            net[width - 1, j] = Node.RemoveLink(net[width - 1, j], 1);
            links[width - 1, j].RemoveLink(1);
        }

        FormNodesGroups();
        ConnectNearestPoints();
        NodesRedefinition();

        GetEndPoints();
        root = MakeTree();
        FormEndPointsGroups2();
    }

    public GlobalNodeInfo GenerateGNI(int x, int y, int difficulty)
    {
        GlobalNodeInfo info = new GlobalNodeInfo();
        info.info = new NetworkNodeInfo();
        info.info.globalAddress = new Vector2Int(x, y);
        info.info.localAddress = new Vector2Int(x, y);
        info.info.nodeDifficulty = difficulty;
        info.info.nodeRole = 7;
        info.info.nodeType = Node.GetType(x, y);

        width = 2 + GMARandom.Range3D(difficulty, (1 + difficulty) * 2, x, y, 1);
        height = 2 + GMARandom.Range3D(difficulty, (1 + difficulty) * 2, x, y, 2);

        this.difficulty = difficulty;

        Debug.Log("Init " + x + " " + y + " " + difficulty + " " + width + " " + height);

        net = new int[width, height];
        links = new Link[width, height];
        roles = new int[width, height];

        haveEntryPoint = false;
        shiftx = GMARandom.Range2D(-1000000, 100000, x + y, (x + 1) * (y + 1));
        shifty = GMARandom.Range2D(-1000000, 100000, x - y, x + y);
        //shiftx = -863620;
        //shifty = 52045;

        Debug.Log(x + " " + y + " " + shiftx + "  " + shifty);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int nodeType = Node.GetType(i + shiftx, j + shifty);
                net[i, j] = nodeType;

                links[i, j] = GetLink(nodeType, new Vector2(i, j));
                links[i, j].nodeType = nodeType;

                roles[i, j] = -1;
            }
        }

        for (int i = 0; i < width; i++)
        {
            net[i, 0] = Node.RemoveLink(net[i, 0], 2);
            links[i, 0].RemoveLink(2);

            //if ((net[i, 0] == 1 || net[i, 0] == 8 || net[i, 0] == 2) && !haveEntryPoint) //TODO entryPoint
            //{
            //    entryPoint.x = i;
            //    entryPoint.y = 0;
            //    haveEntryPoint = true;
            //}

            net[i, height - 1] = Node.RemoveLink(net[i, height - 1], 0);
            links[i, height - 1].RemoveLink(0);
        }

        for (int j = 0; j < height; j++)
        {
            net[0, j] = Node.RemoveLink(net[0, j], 3);
            links[0, j].RemoveLink(3);

            //if ((net[0, j] == 1 || net[0, j] == 2) && !haveEntryPoint)
            //{
            //    entryPoint.x = 0;
            //    entryPoint.y = j;
            //    haveEntryPoint = true;
            //}

            net[width - 1, j] = Node.RemoveLink(net[width - 1, j], 1);
            links[width - 1, j].RemoveLink(1);
        }

        FormNodesGroups();
        ConnectNearestPoints();
        NodesRedefinition();

        GetEndPoints();
        root = MakeTree();
        FormEndPointsGroups2();



        return info;
    }

    public override Vector2Int GetGlobalAddress(Vector2Int localAddress)
    {
        return globalAddress;
    }

    public override int GetNodeDifficulty(int x, int y)
    {
        return difficulty;
    }

    public override int GetNodeRole(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return -1;

        int role = -1;
        //Debug.Log("Get role from " + x + "  " + y);
        role = roles[x, y];

        return role;
    }

    private void FormEndPointsGroups2 ()
    {
        List<List<Node>> groups = new List<List<Node>>();
        List<KeyValuePair<int, int>> handled = new List<KeyValuePair<int, int>>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Node node = new Node(GetNodeType(i, j), i, j);
                if (node.x == entryPoint.x && node.y == entryPoint.y) continue;

                var links = GetLinks(node);
                if (links.Count != 1) continue;

                if (handled.Contains(new KeyValuePair<int, int>(i, j)))
                {
                    //Debug.Log("Skip " + i + " " + j);
                    continue;
                }
                handled.Add(new KeyValuePair<int, int>(i, j));

                //Debug.Log("Handle " + i + " " + j);

                List<Node> group = new List<Node>();
                group.Add(node);

                Node prev = node;
                node = links[0];
                while (node != null)
                {
                    //Debug.Log("Add " + node.x + " " + node.y);
                    group.Add(node);
                    //if (handled.Contains(new KeyValuePair<int, int>(node.x, node.y))) break;
                    handled.Add(new KeyValuePair<int, int>(node.x, node.y));
                    links = GetLinks(node);
                    if (links.Count != 2) break;
                    if (node.x == entryPoint.x && node.y == entryPoint.y) break;
                    //Debug.Log("Node " + node.x + " " + node.y + " has 2 links " + links[0].x + " " + links[0].y + " | " + links[1].x + " " + links[1].y);
                    if (links[0].x == prev.x && links[0].y == prev.y)
                    {
                        //Debug.Log("Select 1");
                        prev = node;
                        node = links[1];
                    }
                    else
                    {
                        //Debug.Log("Select 0");
                        prev = node;
                        node = links[0];
                    }
                }

                groups.Add(group);
            }
        }

        Debug.Log("Found " + groups.Count + " groups");
        //for(int i = 0; i < groups.Count; i++)
        //{
        //    string str = "";
        //    for (int j = 0; j < groups[i].Count; j++) str += "(" + groups[i][j].x + "," + groups[i][j].y + "), ";
        //    Debug.Log(str);
        //}

        for (int i = 0; i < groups.Count; i++)
        {
            string str = "";
            for (int j = 0; j < groups[i].Count; j++) str += "(" + groups[i][j].x + "," + groups[i][j].y + "), ";
            Debug.Log(str);

            var group = groups[i];
            int role = -1;

            int cnt = group.Count - 1;

            for (int j = 0; j <= cnt; j++)
            {
                var node = group[j];

                if (cnt == 1)
                {
                    role = GetLocalNodeType(4, 5, globalAddress.x + globalAddress.y, i, j);//DS or PC
                    Debug.Log("Set " + node.x + " " + node.y + " to " + role + " | " + j);
                }
                else if (cnt == 2)
                {
                    if (j == 0) role = GetLocalNodeType(2, 5, globalAddress.x + globalAddress.y, i, j);// GMARandom.Range3D(2, 5, coord.x + coord.y, i, j);
                    else role = GetLocalNodeType(1, 5, globalAddress.x + globalAddress.y, i, j);// GMARandom.Range3D(1, 5, coord.x + coord.y, i, j);

                    Debug.Log("Set " + node.x + " " + node.y + " to " + role + " | " + j);
                }
                else if (cnt == 3)
                {
                    if (j == cnt - 1) role = 1;//SM
                    else role = GetLocalNodeType(2, 5, globalAddress.x + globalAddress.y, i, j);// GMARandom.Range3D(2, 5, coord.x + coord.y, i, j);

                    Debug.Log("Set " + node.x + " " + node.y + " to " + role + " | " + j);
                }
                else if (cnt > 3)
                {
                    if (j == cnt - 1) role = 1;//SM
                    else if (j == 0) role = GetLocalNodeType(2, 5, globalAddress.x + globalAddress.y, i, j);// GMARandom.Range3D(2, 5, coord.x + coord.y, i, j);
                    else
                    {
                        if (j % 2 == 0) role = GetLocalNodeType(1, 5, globalAddress.x + globalAddress.y, i, j);// GMARandom.Range3D(1, 5, coord.x + coord.y, i, j);
                        else role = GetLocalNodeType(2, 5, globalAddress.x + globalAddress.y, i, j);// GMARandom.Range3D(2, 5, coord.x + coord.y, i, j);
                    }

                    Debug.Log("Set " + node.x + " " + node.y + " to " + role + " | " + j);
                }

                roles[node.x, node.y] = role;//TODO
            }

            if (group.Count > 1) roles[group[cnt].x, group[cnt].y] = 0;//SW
        }

        roles[(int)entryPoint.x, (int)entryPoint.y] = 7;//R

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (net[i, j] == 0) continue;
                if (roles[i, j] >= 0) continue;
                roles[i, j] = GMARandom.Range2D(0, 2, globalAddress.x + i, globalAddress.y + j);//SW or SM
            }
        }

        if (posibleAlarmNodes.Count > 0)
        {
            int n = GMARandom.Range2D(1, Mathf.Min(3, posibleAlarmNodes.Count+1), globalAddress.x, globalAddress.y);
            for (int i = 0; i < n; i++)
            {
                int ind = GMARandom.Range3D(0, posibleAlarmNodes.Count, globalAddress.x, globalAddress.y, i);
                Vector2Int c = posibleAlarmNodes[ind];
                if (roles[c.x, c.y] != 7) roles[c.x, c.y] = 6;//AM
                posibleAlarmNodes.RemoveAt(ind);
            }
        }
    }

    //sw, sm, tc, mm, ds, pc
    private List<int> chances = new List<int>() { 10, 10, 2, 2, 2, 20 };
    private int GetLocalNodeType(int from, int to, int x, int y, int z)
    {
        if (from < 0) from = 0;

        int n = 0;
        for (int i = from; i <= to && i < chances.Count; i++) n += chances[i];

        int rnd = GMARandom.Range3D(0, n, x, y, z);

        int s = 0;
        for (int i = from; i <= to && i < chances.Count; i++)
        {
            if (s + chances[i] > rnd) return i;
            s += chances[i];
        }

        return -1;
    }

    private List<Node> GetLinks(Node node)
    {
        List<Node> links = new List<Node>();

        if (node.HaveUp()) links.Add(new Node(GetNodeType(node.x, node.y + 1), node.x, node.y + 1));
        if (node.HaveRight()) links.Add(new Node(GetNodeType(node.x + 1, node.y), node.x + 1, node.y));
        if (node.HaveDown()) links.Add(new Node(GetNodeType(node.x, node.y - 1), node.x, node.y - 1));
        if (node.HaveLeft()) links.Add(new Node(GetNodeType(node.x - 1, node.y), node.x - 1, node.y));

        return links;
    }

    private TreeNode root;
    private List<Vector2Int> posibleAlarmNodes = new List<Vector2Int>();
    private TreeNode MakeTree()
    {
        TreeNode root = new TreeNode();
        root.coord = new Vector2Int(-1, -1);
        List<Vector2Int> allNodes = new List<Vector2Int>();
        List<TreeNode> queue = new List<TreeNode>();
        Node firstNoneEmpty = null;

        Dictionary<Vector2Int, Vector2Int> loops = new Dictionary<Vector2Int, Vector2Int>();

        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                Node node = new Node(GetNodeType(i, j), i, j);
                if (firstNoneEmpty == null && node.Type != 0) firstNoneEmpty = node;
                var links = GetLinks(node);
                if (links.Count != 1) continue;
                root.coord = new Vector2Int(i, j);
                break;
            }
        }

        if (firstNoneEmpty == null) return null;

        if (root.coord.x < 0) root.coord = new Vector2Int(firstNoneEmpty.x, firstNoneEmpty.y);

        queue.Add(root);
        allNodes.Add(root.coord);
        int ind = 0;
        while (ind < queue.Count)
        {
            TreeNode treeNode = queue[ind];
            ind++;
            Node node = new Node(GetNodeType(treeNode.coord.x, treeNode.coord.y), treeNode.coord.x, treeNode.coord.y);
            var links = GetLinks(node);
            for (int i = 0; i < links.Count; i++)
            {
                Vector2Int coord = new Vector2Int(links[i].x, links[i].y);
                if (allNodes.Contains(coord))
                {
                    if (treeNode.parent != null && treeNode.parent.coord != coord)
                    {
                        if (!loops.ContainsKey(coord) && !loops.ContainsKey(treeNode.coord)) loops.Add(treeNode.coord, coord);
                        //Debug.LogError("Ring! from " + node.x + " " + node.y + " to " + coord.x + " " + coord.y);
                    }
                    continue;
                }
                TreeNode newNode = new TreeNode();
                newNode.coord = coord;
                newNode.parent = treeNode;
                treeNode.links.Add(newNode);
                treeNode.weights.Add(0);
                queue.Add(newNode);
                allNodes.Add(newNode.coord);
            }
        }

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].links.Count > 0) continue;

            TreeNode treeNode = queue[i];

            while (treeNode.parent != null)
            {
                int n = 0;
                for (int j = 0; j < treeNode.weights.Count; j++) n += treeNode.weights[j];
                ind = treeNode.parent.links.IndexOf(treeNode);
                treeNode.parent.weights[ind] = n + 1;
                treeNode = treeNode.parent;
            }
        }

        {
            int n = 0;
            for (int j = 0; j < root.weights.Count; j++) n += root.weights[j];
            int median = n / 2;

            TreeNode best = root;
            int min = median;
            for(int i = 0; i < queue.Count; i++)
            {
                n = 0;
                for (int j = 0; j < queue[i].weights.Count; j++) n += queue[i].weights[j];
                if(Mathf.Abs(n - median) < min)
                {
                    min = Mathf.Abs(n - median);
                    best = queue[i];
                }
            }

            entryPoint = best.coord;
        }

        foreach (var kvp in loops)
        {
            int x1 = kvp.Key.x;
            int y1 = kvp.Key.y;
            int x2 = kvp.Value.x;
            int y2 = kvp.Value.y;

            net[x1, y1] = Node.RemoveLink(net[x1, y1], Node.Direction(x1, y1, x2, y2));
            net[x2, y2] = Node.RemoveLink(net[x2, y2], Node.Direction(x2, y2, x1, y1));
        }

        //Debug.LogError("Tree");
        //for(int i = 0; i < queue.Count; i++)
        //{
        //    int n = 0;
        //    for (int j = 0; j < queue[i].weights.Count; j++) n += queue[i].weights[j];
        //    Debug.Log(queue[i].coord + " | " + n);
        //}

        posibleAlarmNodes.Clear();
        for(int i = 0; i < queue.Count; i++)
        {
            if (queue[i].links.Count > 3) posibleAlarmNodes.Add(queue[i].coord);
        }

        if(posibleAlarmNodes.Count < 2)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].links.Count > 2) posibleAlarmNodes.Add(queue[i].coord);
            }
        }

        return root;
    }

    public override Vector2 GetEntryPoint()
    {
        return entryPoint;
    }


    private void NodesRedefinition()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if ((net[i, j] == 1 || net[i, j] == 2 || net[i, j] == 8 || net[i, j] == 4) && !haveEntryPoint) //TODO entryPoint
                {
                    entryPoint.x = i;
                    entryPoint.y = j;
                    haveEntryPoint = true;
                }
            }
        }
    }


    private void FormeNodesGroups2(int[,] net)
    {
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        stopWatch.Start();

        int w = net.GetLength(0);
        int h = net.GetLength(1);
        int[,] map = new int[w, h];
        List<List<KeyValuePair<int, int>>> result_groups = new List<List<KeyValuePair<int, int>>>();

        result_groups.Clear();

        for(int i = 0; i < w; i++)
        {
            for(int j = 0; j < h; j++)
            {
                map[i, j] = -1;
            }
        }

        for (int i = 0; i < w; i++)
        {
            for(int j = 0; j < h; j++)
            {
                var node = net[i, j];

                if (node == 0) continue;

                List<KeyValuePair<int, int>> nodes = new List<KeyValuePair<int, int>>();
                nodes.Add(new KeyValuePair<int, int>(i, j));
                if (Node.HaveLink(node, 0)) nodes.Add(new KeyValuePair<int, int>(i, j + 1));
                if (Node.HaveLink(node, 1)) nodes.Add(new KeyValuePair<int, int>(i + 1, j));
                if (Node.HaveLink(node, 2)) nodes.Add(new KeyValuePair<int, int>(i, j - 1));
                if (Node.HaveLink(node, 3)) nodes.Add(new KeyValuePair<int, int>(i - 1, j));

                List<int> groups = new List<int>();
                for (int k = 0; k < nodes.Count; k++)
                {
                    var n = nodes[k];
                    int g = map[n.Key, n.Value];
                    if (g >= 0 && !groups.Contains(g)) groups.Add(g);
                }

                System.Func<List<int>, int> JoinGroups = (list) =>
                {
                    int res = list[0];
                    var group1 = result_groups[list[0]];
                    for (int k = 1; k < list.Count; k++)
                    {
                        var group2 = result_groups[list[k]];
                        for(int l = 0; l < group2.Count; l++)
                        {
                            if (!group1.Contains(group2[l])) group1.Add(group2[l]);
                            //else Debug.LogError("???");
                            map[group2[l].Key, group2[l].Value] = res;
                        }
                        group2.Clear();
                    }

                    return res;
                };

                int groupind = 0;
                if (groups.Count > 1)
                {
                    groupind = JoinGroups(groups);
                }
                else if (groups.Count == 1)
                {
                    groupind = groups[0];
                }
                else
                {
                    groupind = result_groups.Count;
                    result_groups.Add (new List<KeyValuePair<int, int>>());
                }

                var group = result_groups[groupind];

                for (int k = 0; k < nodes.Count; k++)
                {
                    var n = nodes[k];
                    map[n.Key, n.Value] = groupind;
                    var kvp = new KeyValuePair<int, int>(n.Key, n.Value);
                    if (!group.Contains(kvp)) group.Add(kvp);
                }
            }
        }

        for(int i = 0; i < result_groups.Count; i++)
        {
            if(result_groups[i].Count == 0)
            {
                result_groups.RemoveAt(i);
                i--;
            }
        }

        stopWatch.Stop();
        long ts = stopWatch.ElapsedMilliseconds;
        Debug.LogError(ts + "_______________________________");

        Debug.Log("Found " + result_groups.Count + " groups");
        for (int i = 0; i < result_groups.Count; i++)
        {
            var g = result_groups[i];
            for (int j = 0; j < g.Count; j++)
            {
                Debug.Log(i + " " + j + ") " + g[j].Key + " " + g[j].Value);
            }
        }
    }


    private Link GetLink(int type, Vector2 pos)
    {
        List<int> directions = new List<int>();

        if (Node.HaveLink(type, 0)) directions.Add(0);
        if (Node.HaveLink(type, 1)) directions.Add(1);
        if (Node.HaveLink(type, 2)) directions.Add(2);
        if (Node.HaveLink(type, 3)) directions.Add(3);

        return new Link(directions, pos);
    }


    List<Link> savedLink = new List<Link>();
    List<List<Link>> groups = new List<List<Link>>();
    List<Link> pointsGroup = new List<Link>();
    List<Link> allPoints = new List<Link>();

    public void FormNodesGroups()
    {
        //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        //stopWatch.Start();

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Debug.LogWarning("For cycle + coord " + i + "-" + j + "/ nodeType " + links[i, j].nodeType);
                    if (links[i, j].nodeType == 0 || links[i, j].directions.Count == 0) continue;

                    CheckLinks(links[i, j]);
                }
            }

        //Debug.LogError("Print! GroupsCount " + groups.Count + " groups[0].count " + groups[0].Count);

        iterations = 0;
    }

    private void ConnectNearestPoints()
    {
        //Debug.LogError("Groups count= " + groups.Count + " groups[0].count " + groups[0].Count);
        if (groups.Count <= 1) { /*Debug.LogError("Groups count= " + groups.Count + " Return!");*/ return; }

        List<Link> checkedList_1 = new List<Link>();
        List<Link> checkedList_2 = new List<Link>();

        int preDeltaX = int.MaxValue;
        int preDeltaY = int.MaxValue;

        KeyValuePair<Vector2, Vector2> couplePoints = new KeyValuePair<Vector2, Vector2>(Vector2.zero, Vector2.zero);
        List<KeyValuePair<Vector2, Vector2>> nearestCouplePoints = new List<KeyValuePair<Vector2, Vector2>>();

        for (int i = 0; i < groups.Count - 1; i++)
        {
            checkedList_1.AddRange(groups[i]);
            checkedList_2 = groups[i + 1];

            for (int j = 0; j < checkedList_1.Count; j++)
            {
                for (int k = 0; k < checkedList_2.Count; k++)
                {
                    if (checkedList_1[j].position == checkedList_2[k].position)
                    {
                        //Debug.LogError("Говнище страшное приключилось!");
                        //Debug.LogWarning("GroupsCount " + groups.Count + " checkedList_1.Count " + checkedList_1.Count + " checkedList_2.Count " + checkedList_2.Count);
                        continue;
                    }

                    int posX_1 = (int)checkedList_1[j].position.x;
                    int posY_1 = (int)checkedList_1[j].position.y;
                    int posX_2 = (int)checkedList_2[k].position.x;
                    int posY_2 = (int)checkedList_2[k].position.y;

                    int deltaX = Mathf.Abs(posX_1 - posX_2);
                    int deltaY = Mathf.Abs(posY_1 - posY_2);

                    if (deltaX <= preDeltaX && deltaY <= preDeltaY)
                    {
                        preDeltaX = deltaX;
                        preDeltaY = deltaY;
                        couplePoints = new KeyValuePair<Vector2, Vector2>(checkedList_1[j].position, checkedList_2[k].position);
                    }
                }
            }

            nearestCouplePoints.Add(couplePoints);
            preDeltaX = int.MaxValue;
            preDeltaY = int.MaxValue;
        }

        Debug.LogError("Finish! CoupleCount= " + nearestCouplePoints.Count);

        for (int i = 0; i < nearestCouplePoints.Count; i++)
        {
            Debug.LogWarning(nearestCouplePoints[i].Key + " // " + nearestCouplePoints[i].Value);
            GetConnect(nearestCouplePoints[i].Key, nearestCouplePoints[i].Value);
        }

        nearestCouplePoints.Clear();
        groups.Clear();
    }


    private int iterations = 0;
    private int pointsCount = 0;
    private void CheckLinks(Link startLink)
    {
        iterations++;
        if (iterations > 1000) { Debug.LogError("Return, long cycle"); return; }

        if (!savedLink.Contains(startLink)) savedLink.Add(startLink);
        if (!pointsGroup.Contains(startLink)) { pointsGroup.Add(startLink); pointsCount++; }

        int dir = startLink.directions[0];
        int backDir = GetBackDirection(dir);

        startLink.RemoveLink(dir);
        if (startLink.closed) savedLink.Remove(startLink);

        int nextPosX = startLink.GetX();
        int nextPosY = startLink.GetY();

        if (dir == 0) nextPosY += 1;
        if (dir == 1) nextPosX += 1;
        if (dir == 2) nextPosY -= 1;
        if (dir == 3) nextPosX -= 1;

        links[nextPosX, nextPosY].RemoveLink(backDir);
        //Debug.Log("StartLink " + startLink.position + " nextLink " + links[nextPosX, nextPosY].position + " //startPosDirCount " + startLink.directions.Count + " nextLinkDirCount " + links[nextPosX, nextPosY].directions.Count);
        if (!pointsGroup.Contains(links[nextPosX, nextPosY])) { pointsGroup.Add(links[nextPosX, nextPosY]); pointsCount++; }
        //Debug.Log("PointsCount " + pointsCount);

        if (links[nextPosX, nextPosY].closed)
        {
            if (savedLink.Contains(links[nextPosX, nextPosY])) { savedLink.Remove(links[nextPosX, nextPosY]);/* Debug.Log("DelSavedLink");*/ }
            //Debug.Log("NextLink close! savedLinkCount= " + savedLink.Count);
            if (savedLink.Count < 1)
            {
                List<Link> tempList = new List<Link>();
                tempList.AddRange(pointsGroup);

                groups.Add(tempList);
                allPoints.AddRange(tempList);
                pointsGroup.Clear();
                //Debug.LogError("savedLink.Count < 1  __Return");
                return;
            }
            else
            {
                CheckLinks(savedLink[0]);
            }
        }
        else
        {
            //Debug.Log("NextLink not closed, recursion");
            CheckLinks(links[nextPosX, nextPosY]);
        }
    }

    public int GetBackDirection(int direction)
    {
        int backDir = -1;

        if (direction == 0) backDir = 2;
        if (direction == 1) backDir = 3;
        if (direction == 2) backDir = 0;
        if (direction == 3) backDir = 1;
        return backDir;
    }

    private void GetConnect(Vector2 point_1, Vector2 point_2)
    {
        int directionPoint_1 = 0;
        int directionPoint_2 = 0;

        int directionsCount = 0;
        bool up = false;
        bool right = false;
        bool down = false;
        bool left = false;

        if (point_1.x < point_2.x) { right = true; directionsCount++; }
        if (point_1.x > point_2.x) { left = true; directionsCount++; }
        if (point_1.y < point_2.y) { up = true; directionsCount++; }
        if (point_1.y > point_2.y) { down = true; directionsCount++; }

        if (directionsCount > 1) { Debug.LogError("No direct connection! Points: " + point_1 + " // " + point_2 + " Return!"); return; } 

        if (up) { directionPoint_1 = 0; directionPoint_2 = GetBackDirection(0); }
        if (right) { directionPoint_1 = 1; directionPoint_2 = GetBackDirection(1); }
        if (down) { directionPoint_1 = 2; directionPoint_2 = GetBackDirection(2); }
        if (left) { directionPoint_1 = 3; directionPoint_2 = GetBackDirection(3); }

        int deltaX = (int)(point_1.x - point_2.x);
        int deltaY = (int)(point_1.y - point_2.y);

        Debug.LogError(up + " / " + right + " / " + down + " / " + left + " delta x/y= " + deltaX + "//" + deltaY + " dir 1/2= " + directionPoint_1 + "//" + directionPoint_2);
        Debug.LogError((int)point_1.x + " " + (int)point_1.y + " " + width + " " + height);
        Debug.LogError("NetCount " + net.Length);
        net[(int)point_1.x, (int)point_1.y] = Node.AddLink(net[(int)point_1.x, (int)point_1.y], directionPoint_1);
        net[(int)point_2.x, (int)point_2.y] = Node.AddLink(net[(int)point_2.x, (int)point_2.y], directionPoint_2);

        if (deltaX > 1 || deltaX < -1)
        {
            Debug.LogError("Big delta! deltaX= " + deltaX + " delatY " + deltaY);

            for (int i = 1; i < Mathf.Abs(deltaX); i++)
            {
                if (deltaX < 0)
                {
                    //Debug.Log("NodeType " + net[(int)point_1.x + i, (int)point_1.y]);
                    net[(int)point_1.x + i, (int)point_1.y] = Node.AddLink(net[(int)point_1.x + i, (int)point_1.y], directionPoint_1);
                    net[(int)point_1.x + i, (int)point_1.y] = Node.AddLink(net[(int)point_1.x + i, (int)point_1.y], directionPoint_2);
                    //Debug.LogWarning("NodeType " + net[(int)point_1.x + i, (int)point_1.y]);
                }
                else if(deltaX > 0)
                {
                    //Debug.Log("NodeType " + net[(int)point_1.x - i, (int)point_1.y]);
                    net[(int)point_1.x - i, (int)point_1.y] = Node.AddLink(net[(int)point_1.x - i, (int)point_1.y], directionPoint_1);
                    net[(int)point_1.x - i, (int)point_1.y] = Node.AddLink(net[(int)point_1.x - i, (int)point_1.y], directionPoint_2);
                    //Debug.LogWarning("NodeType " + net[(int)point_1.x - i, (int)point_1.y]);
                }
            }
        }

        if(deltaY > 1 || deltaY < -1)
        {
            Debug.LogError("Big delta! deltaX= " + deltaX + " delatY " + deltaY);
            for (int i = 1; i < Mathf.Abs(deltaY); i++)
            {
                if (deltaY < 0)
                {
                    //Debug.Log("NodeType " + net[(int)point_1.x, (int)point_1.y + i]);
                    net[(int)point_1.x, (int)point_1.y + i] = Node.AddLink(net[(int)point_1.x, (int)point_1.y + i], directionPoint_1);
                    net[(int)point_1.x, (int)point_1.y + i] = Node.AddLink(net[(int)point_1.x, (int)point_1.y + i], directionPoint_2);
                    //Debug.LogWarning("NodeType " + net[(int)point_1.x, (int)point_1.y + i]);
                }
                else if (deltaY > 0)
                {
                    //Debug.Log("NodeType " + net[(int)point_1.x, (int)point_1.y - i]);
                    net[(int)point_1.x, (int)point_1.y - i] = Node.AddLink(net[(int)point_1.x, (int)point_1.y - i], directionPoint_1);
                    net[(int)point_1.x, (int)point_1.y - i] = Node.AddLink(net[(int)point_1.x, (int)point_1.y - i], directionPoint_2);
                    //Debug.LogWarning("NodeType " + net[(int)point_1.x, (int)point_1.y - i]);
                }
            }
        }
    }


    private bool OneConnection(int x, int y)
    {
        if (net[x, y] == 1 || net[x, y] == 2 || net[x, y] == 4 || net[x, y] == 8)
        {
            return true;
        }

        return false;
    }


    private List<Vector2Int> endPoints = new List<Vector2Int>();
    private void GetEndPoints()
    {
        endPoints.Clear();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                links[i, j] = GetLink(net[i, j], new Vector2(i, j));
                links[i, j].nodeType = net[i, j];

                if (net[i, j] == 0) continue;

                if(OneConnection(i,j))
                {
                    endPoints.Add(new Vector2Int(i, j));
                    //Debug.LogWarning("Edge: " + endPoints[endPoints.Count - 1]);
                }
            }
        }
    }

    private List<List<Vector2Int>> endPointsGroups = new List<List<Vector2Int>>();
    private void _FormEndPointsGroups()
    {
        endPointsGroups.Clear();

        for (int i = 0; i < endPoints.Count; i++)
        {
            List<Vector2Int> group = new List<Vector2Int>();
            group.Add(endPoints[i]);
            Debug.LogError("For " + i + endPoints[i]);

            if (links[endPoints[i].x, endPoints[i].y].directions.Count > 1) continue;

            Vector2Int nextPoint = GetNextPoint(endPoints[i]);
            Link nextLink = links[nextPoint.x, nextPoint.y];

            int counter = 0;
            while (nextLink.directions.Count < 2 && counter < 100)
            {
                counter++;
                group.Add(nextPoint);

                Debug.LogWarning("For " + i + nextPoint);

                nextPoint = GetNextPoint(endPoints[i]);
                nextLink = links[nextPoint.x, nextPoint.y];
            }

            endPointsGroups.Add(group);
        }

        //foreach (var item in endPointsGroups)
        //{
        //    Debug.LogError(item.Count);

        //    foreach (var _item in item)
        //    {
        //        Debug.LogWarning(_item);
        //    }
        //}
    }

    private Vector2Int GetNextPoint(Vector2Int startPos)
    {
        //if (links[vec.x, vec.y].directions.Count >) return new Vector2Int(-1, -1);
        int direction = links[startPos.x, startPos.y].directions[0];

        Vector2Int nextPoint = new Vector2Int(-1,-1);

        if (direction == 0)
        {
            nextPoint.x = startPos.x;
            nextPoint.y = startPos.y + 1;
        }
        else if (direction == 1)
        {
            nextPoint.x = startPos.x + 1;
            nextPoint.y = startPos.y;
        }
        else if (direction == 2)
        {
            nextPoint.x = startPos.x;
            nextPoint.y = startPos.y - 1;
        }
        else if (direction == 3)
        {
            nextPoint.x = startPos.x - 1;
            nextPoint.y = startPos.y;
        }

        return nextPoint;
    }

    public override int GetNodeType(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return 0;

        return net[x, y];
    }

    public override bool CanGo(Direction dir, int fromX, int fromY)
    {
        if (fromX < 0 || fromX >= width || fromY < 0 || fromY >= height) return false;

        SecurityModule sm = PlayerController.instance.CurrentNode.GetAppByName("SecurityModule") as SecurityModule;

        //if (sm != null) Debug.LogError("Hacked= " + sm.hacked);

        if (sm != null && !sm.hacked)
        {
            int prevX = PlayerController.instance.prevX;
            int prevY = PlayerController.instance.prevY;

            int backDirection = -1;

            int shiftX = prevX - fromX;
            int shiftY = prevY - fromY;

            if (shiftY > 0) backDirection = 1;
            else if (shiftX > 0) backDirection = 2;
            else if (shiftY < 0) backDirection = 3;
            else if (shiftX < 0) backDirection = 4;

            //Debug.LogError(prevX + " " + prevY + " " + fromX + " " + fromY + " " + backDirection + " " + (int)dir);

            return (int)dir == backDirection;
        }

        var node = new Node(net[fromX, fromY]);

        if (dir == Direction.up)
        {
            return node.HaveUp();
        }
        else if (dir == Direction.right)
        {
            return node.HaveRight();
        }
        else if (dir == Direction.down)
        {
            return node.HaveDown();
        }
        else if (dir == Direction.left)
        {
            return node.HaveLeft();
        }

        return false;
    }
}
