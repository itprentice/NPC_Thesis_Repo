using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class GPTAgent : MonoBehaviour
{
    [SerializeField]
    private Camera cam = null;
    [SerializeField]
    private int numXRays = 100;
    [SerializeField]
    private int numYRays = 100;
    [SerializeField]
    private List<string> yCategories;
    [SerializeField]
    private List<string> xCategories;
    [SerializeField]
    private GPTAPIHandler apiHandler;
    [SerializeField]
    private string prologue;
    [SerializeField]
    private string epilogue;
    //number of times to analyze scene
    [SerializeField] private int repetitionCount;

    private UnityWebRequest currentRequest = null;
    private int currentRepetition;
    private string storedScenario;
    private float startTime;
    private List<string> resultRows;
    private string resultPath;
    /**
     * scan for game objects that contain TextDescription components within the view of the agent camera
     * input: None
     * output: a dictionary containing the GameObjects and their position in the viewport
     **/
    Dictionary<DescriptionNode, Vector2> scanForDescriptionsWithCoords()
    {
        //add 2 to the denominator of the deltas in order to leave a border at the edge of the screen where no rays are cast
        float deltaX = 1 / (float)(numXRays + 2), deltaY = 1 / (float)(numYRays + 2);
        float x = 0, y = 0;

        Dictionary<GameObject, (float xSum, float ySum, int count)> scanDict = new();
        for (int i = 0; i < numYRays; i++)
        {
            y += deltaY;
            for (int j = 0; j < numXRays; j++)
            {
                x += deltaX;
                //Debug.Log(x + " " + y);
                Ray r = cam.ViewportPointToRay(new Vector3(x, y, 0));
                //Debug.Log(r);
                if (Physics.Raycast(r, out RaycastHit hit))
                {
                    //Debug.Log("hit!");
                    //Debug.Log(hit.collider.gameObject.GetInstanceID());
                    if (scanDict.ContainsKey(hit.collider.gameObject))
                    {
                        var (xSum, ySum, count) = scanDict[hit.collider.gameObject];

                        xSum += x;
                        ySum += y;
                        count++;
                        scanDict[hit.collider.gameObject] = (xSum, ySum, count);
                    }
                    else
                    {
                        scanDict[hit.collider.gameObject] = (x, y, 1);
                    }
                }
            }
            x = 0.0f;
        }
        Dictionary<DescriptionNode, Vector2> ret = new();
        foreach (var scanItem in scanDict)
        {
            //TODO: filter by TextDescription components
            if (scanItem.Key.TryGetComponent<DescriptionNode>(out DescriptionNode t))
            {
                ret[t] = new Vector2(scanItem.Value.xSum / scanItem.Value.count, scanItem.Value.ySum / scanItem.Value.count);
                //Debug.Log(t.GetHashCode());
            }
            //Debug.Log(scanItem.Key.name + ": " + ret[scanItem.Key]);
        }
        return ret;
    }
    /**
     * scan for game objects that contain TextDescription components within the view of the agent camera
     * input: None
     * output: a HashSet containing the discovered description nodes
     **/
    HashSet<DescriptionNode> scanForDescriptions()
    {
        //add 2 to the denominator of the deltas in order to leave a border at the edge of the screen where no rays are cast
        float deltaX = 1 / (float)(numXRays + 2), deltaY = 1 / (float)(numYRays + 2);
        float x = 0, y = 0;

        HashSet<GameObject> scanSet = new();
        for (int i = 0; i < numYRays; i++)
        {
            y += deltaY;
            for (int j = 0; j < numXRays; j++)
            {
                x += deltaX;
                //Debug.Log(x + " " + y);
                Ray r = cam.ViewportPointToRay(new Vector3(x, y, 0));
                //Debug.Log(r);
                if (Physics.Raycast(r, out RaycastHit hit))
                {
                    scanSet.Add(hit.collider.gameObject);
                }
            }
            x = 0.0f;
        }
        HashSet<DescriptionNode> ret = new();
        foreach (var scanItem in scanSet)
        {
          
            if (scanItem.TryGetComponent<DescriptionNode>(out DescriptionNode t))
            {
                ret.Add(t);
                //Debug.Log(t.GetHashCode());
            }
            //Debug.Log(scanItem.Key.name + ": " + ret[scanItem.Key]);
        }
        return ret;
    }
    /**
     * categorizes the viewport positions of the supplied node dictionary and returns them in a [y, x] matrix where each element is a list of nodes in that category
     * input: none
     * output: [y,x] matrix of lists containing the description nodes in each category
     **/
    List<DescriptionNode>[,] categorizeViewportPositions(int xCatCount, int yCatCount, Dictionary<DescriptionNode, Vector2> descriptions)
    {
        List<DescriptionNode>[,] catMatrix = new List<DescriptionNode>[yCatCount, xCatCount];
        //define coordinate bounds where any coordinate < bound[i] falls in category i
        float[] xBounds = new float[xCatCount], yBounds = new float[yCatCount];
        for (int i = 0; i < xCatCount; i++)
        {
            xBounds[i] = (i + 1) / (float)xCatCount;
        }
        for (int j = 0; j < yCatCount; j++)
        {
            yBounds[j] = (j + 1) / (float)yCatCount;
        }

        foreach (KeyValuePair<DescriptionNode, Vector2> item in descriptions)
        {
            for (int j = 0; j < yCatCount; j++)
            {
                if (item.Value.y < yBounds[j])
                {
                    for (int i = 0; i < xCatCount; i++)
                    {
                        if (item.Value.x < xBounds[i])
                        {
                            //found matching category
                            catMatrix[j, i].Add(item.Key);
                            break;
                        }
                    }
                    break;
                }
            }
        }
        return catMatrix;
    }
    /**
     * categorizes the viewport positions of the supplied node dictionary and returns them in a [y, x] matrix where each element is a list of nodes in that category
     * note: this overload converts world positions to viewport in order to categorize each node
     * input: xCatCount - number of horizontal positional categories, yCatCount - number of vertical positional categories, descriptions - the description nodes in this scene
     * output: [y,x] matrix of lists containing the description nodes in each category
     **/
    List<DescriptionNode>[,] categorizeViewportPositions(int xCatCount, int yCatCount, HashSet<DescriptionNode> descriptions)
    {
        //in these scenarios, its either a 1xn, nx1, or 1x1 grid
        if (xCatCount <= 1)
        {
            xCatCount = 1;
        }
        if (yCatCount <= 1)
        {
            yCatCount = 1;
        }

        List<DescriptionNode>[,] catMatrix = new List<DescriptionNode>[yCatCount, xCatCount];
        //initialize all lists
        for (int j = 0; j < yCatCount; j++)
        {
            for (int i = 0; i < xCatCount; i++)
            {
                catMatrix[j, i] = new List<DescriptionNode>();
            }
        }
        //define coordinate bounds where any coordinate < bound[i] falls in category i
        float[] xBounds = new float[xCatCount - 1], yBounds = new float[yCatCount - 1];
        for (int i = 0; i < xCatCount - 1; i++)
        {
            xBounds[i] = (i + 1) / (float)xCatCount;
        }
        for (int j = 0; j < yCatCount - 1; j++)
        {
            yBounds[j] = (j + 1) / (float)yCatCount;
        }

        foreach (var node in descriptions)
        {
            for (int j = 0; j < yCatCount; j++)
            {
                Vector3 pos = cam.WorldToViewportPoint(node.gameObject.transform.position);

                if (j == yCatCount - 1 || pos.y < yBounds[j])
                {
                    for (int i = 0; i < xCatCount; i++)
                    {
                        if (i == xCatCount - 1 || pos.x < xBounds[i])
                        {
                            catMatrix[j, i].Add(node);
                            break;
                        }
                    }
                    break;
                }
            }
        }
        
        
        return catMatrix;
    }
    /**
     *  traverses the tree(s) that the provided nodes are in and returns only the roots
     *  input: descriptions - set of nodes in the scene
     *  output: set of root nodes in the scene
     **/
    HashSet<DescriptionNode> reduceToRoots(HashSet<DescriptionNode> descriptions)
    {
        HashSet<DescriptionNode> roots = new();
        foreach(var node in descriptions)
        {
            roots.Add(node.findRoot());
        }
        return roots;
    }   
    /**
     * triggers the necessary tree walks to compute a full description of the scene in front of the camera
     **/
    public string generateSceneDescription()
    {
        //flags to remove placeholders if necessary
        bool yRemovePlaceholder = false, xRemovePlaceholder = false;
        //add placeholder category if either coordinate is empty
        if (yCategories.Count == 0)
        {
            yCategories.Add("");
            yRemovePlaceholder = true;
        }
        if (xCategories.Count == 0)
        {
            xCategories.Add("");
            xRemovePlaceholder = true;
        }

        var descSet = scanForDescriptions();
        HashSet<DescriptionNode> roots = reduceToRoots(descSet);
        List<DescriptionNode>[,] categorizedDescs = categorizeViewportPositions(xCategories.Count, yCategories.Count, roots);
        string sceneDescription = "";

        for (int j = 0; j < yCategories.Count; j++)
        {
            for (int i = 0; i < xCategories.Count; i++)
            {
                sceneDescription += yCategories[j] + " " + xCategories[i] + ": ";
                if (categorizedDescs[j, i].Count == 0)
                {
                    sceneDescription += "nothing";
                }
                else
                {
                    foreach (var node in categorizedDescs[j, i])
                    {
                        sceneDescription += node.inorderRepresentation() + ", ";
                    }
                    sceneDescription = sceneDescription.Remove(sceneDescription.Length - 2);
                }

                sceneDescription += ". ";
            }
        }
        //remove placeholders (not really necessary for runtime, but editor complains due to inconsistent state)
        if (yRemovePlaceholder)
        {
            yCategories.Clear();
        }
        if (xRemovePlaceholder)
        {
            xCategories.Clear();
        }
        return sceneDescription;
    }

    /**
     * creates full description of the scene, including prologue and epilogue context
     **/
    public string generateScenario()
    {
        
        string sceneDescription = generateSceneDescription();
        string fullScenario = prologue + " The scene is described as follows. " + sceneDescription + epilogue;
        
        return fullScenario;
    }
    /**
     * creates request to GPT API given the full scenario text
     * input: scenario - the text describing the scenario
     * output: web request to GPT API
     **/
    public UnityWebRequest makeRequestForScene(string scenario)
    {
        
        //TODO: write code that sends request to GPT, receive response in Update loop
        if (apiHandler != null)
        {
            currentRequest = apiHandler.makeRequest("user", scenario);
            //currentRequest.SendWebRequest();
        }

        
        return currentRequest;
    }
    // Use this for initialization
    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        yCategories ??= new List<string>();
        xCategories ??= new List<string>();

        currentRequest = null;
        currentRepetition = 0;
        //analyze scene at the start
        storedScenario = generateScenario();
        Debug.Log(storedScenario);
        //Debug.Log(sceneDescription);
        resultPath = Path.Combine(Directory.GetCurrentDirectory(), "results.csv");
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "scenario.txt"), storedScenario);
        File.WriteAllText(resultPath, "option,reasoning,time (seconds)"+Environment.NewLine);
        resultRows ??= new List<string>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentRequest is null)
        {
            if (currentRepetition < repetitionCount)
            {
                currentRequest = makeRequestForScene(storedScenario);
                currentRequest.SendWebRequest();
                startTime = Time.realtimeSinceStartup;
                Debug.Log(currentRepetition);
            }
            else
            {
                //done doing analysis
                //do this because 
                if (resultRows is not null && resultRows.Count > 0)
                {
                    File.AppendAllLines(resultPath, resultRows);
                    resultRows.Clear();
                }
                Debug.Log("done!");
                //stop running since analysis is done
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        else if (currentRequest.isDone)
        {
            string gptResponse = apiHandler.getResponseMessage(currentRequest);
            //Debug.Log(gptResponse);

            currentRequest = null;
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            string[] row = gptResponse.Split(':');
            row[0] = new string(row[0].Where(c => char.IsDigit(c)).ToArray()); //isolate number of option chosen
            row[1] = row[1].Replace("\"", "\"\""); //make sure any quotes in the string doesn't mess up the csv column
            resultRows.Add(row[0] + ",\"" + row[1] + "\"," + elapsedTime);
            currentRepetition++; //increase count after finishing an analysis loop
        }
        
    }
}