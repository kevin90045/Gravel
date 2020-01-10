using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dummiesman;
using System.IO;
using System;
using System.Xml;

public class DropRock : MonoBehaviour
{
    public Transform Stone;
    public int GravelCount = 0;
    public int SceneCount = 0;
    public bool Finish = false;
    public int NumOfGravel = 100;
    public int NumOfScene = 1;
    public float CheckTimeInterval = 5; // unit: second
    public float DropTimeInterval = 0.1f; // unit: second
    public int StopGravelsNum = 0;
    public float TimeOutThreshold = 60; // unit: second

    public float Xmax = 3;
    public float Xmin = -3;
    public float Zmax = 3;
    public float Zmin = -3;
    public float Y = 3;

    public string FilePath;
    private string[] FileNames;
    private System.Random rnd;
    private bool FinishDisplayed = false;
    private List<Vector3> Positions, Rotations;
    private List<string> PickFileNames;
    private float TimeAccumulated = 0;

    // Awake call when object is create
    void Awake()
    {
        Debug.Log("Awake");

        rnd = new System.Random(Guid.NewGuid().GetHashCode());

        FileNames = Directory.GetFiles(@FilePath);
        Debug.Log("Totally " + FileNames.Length + " gravels");

        Positions = new List<Vector3>();
        Rotations = new List<Vector3>();
        PickFileNames = new List<string>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start Scene " + SceneCount.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        if (!Finish)
        {
            if (GravelCount < NumOfGravel)
            {
                if (TimeAccumulated > DropTimeInterval)
                {
                    string fileName = FileNames[rnd.Next() % FileNames.Length];
                    GameObject loadedObject = new OBJLoader().Load(fileName).transform.GetChild(0).gameObject;
                    PickFileNames.Add(fileName);

                    // add Rigidbody
                    Rigidbody rb = loadedObject.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.isKinematic = false;

                    // add MeshCollider
                    MeshCollider mc = loadedObject.AddComponent<MeshCollider>();
                    mc.convex = true;

                    // set parent
                    loadedObject.transform.parent.parent = Stone;

                    // set position
                    float X = Convert.ToSingle(rnd.NextDouble() * Math.Abs(Xmax - Xmin) + Xmin);
                    float Z = Convert.ToSingle(rnd.NextDouble() * Math.Abs(Zmax - Zmin) + Zmin);
                    loadedObject.transform.position = new Vector3(X, Y, Z);

                    // add position and rotation
                    Positions.Add(Vector3.zero);
                    Rotations.Add(Vector3.zero);

                    GravelCount++;
                    TimeAccumulated = 0;
                }
            }
            else if (TimeAccumulated > CheckTimeInterval)
            {
                if (TimeAccumulated > TimeOutThreshold)
                {
                    Debug.Log("Time out! Just output results directly...");
                    Finish = true;
                }
                else
                {
                    Debug.Log("Checking...");
                    Finish = true;
                    for (int i = Positions.Count - 1; i >= 0; i--)
                    {
                        if (CompareVector(Positions[i], Stone.GetChild(i).GetChild(0).position) &&
                            CompareVector(Rotations[i], Stone.GetChild(i).GetChild(0).eulerAngles))
                        {
                            Positions.RemoveAt(i);
                            Rotations.RemoveAt(i);
                            StopGravelsNum++;
                        }
                        else
                        {
                            Finish = false;

                            var newPosition = Stone.GetChild(i).GetChild(0).position;
                            Positions[i] = new Vector3(newPosition.x, newPosition.y, newPosition.z);

                            var newRotation = Stone.GetChild(i).GetChild(0).eulerAngles;
                            Rotations[i] = new Vector3(newRotation.x, newRotation.y, newRotation.z);
                        }
                    }
                }
                TimeAccumulated = 0;
            }
            TimeAccumulated += Time.deltaTime;
        }
        else
        {
            if (!FinishDisplayed)
            {
                WriteCSV("scene_" + SceneCount.ToString() + ".csv");
                WriteXML("scene_" + SceneCount.ToString() + ".xml");

                Debug.Log("Finish");
                FinishDisplayed = true;
                SceneCount++;

                // reset
                if (SceneCount < NumOfScene)
                {
                    Finish = false;
                    Positions = new List<Vector3>();
                    Rotations = new List<Vector3>();
                    PickFileNames = new List<string>();
                    GravelCount = 0;
                    TimeAccumulated = 0;
                    FinishDisplayed = false;

                    foreach (Transform child in Stone)
                    {
                        GameObject.Destroy(child.gameObject);
                    }

                    Debug.Log("Start Scene " + SceneCount.ToString());
                }
            }
        }
    }

    private void WriteXML(string filename)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlElement root = xmlDoc.CreateElement("root");

        for (int i = 0; i < Stone.childCount; i++)
        {
            var position = Stone.GetChild(i).GetChild(0).position;
            var rotation = Stone.GetChild(i).GetChild(0).eulerAngles;

            XmlElement part = xmlDoc.CreateElement("part");

            // objloader
            XmlElement objLoader = xmlDoc.CreateElement("filter");
            objLoader.SetAttribute("type", "objloader");

            XmlElement objLoaderParam1 = xmlDoc.CreateElement("param");
            objLoaderParam1.SetAttribute("type", "string");
            objLoaderParam1.SetAttribute("key", "filepath");
            objLoaderParam1.SetAttribute("value", Path.Combine(Path.GetFileName(Path.GetDirectoryName(PickFileNames[i])), Path.GetFileName(PickFileNames[i])));

            XmlElement objLoaderParam2 = xmlDoc.CreateElement("param");
            objLoaderParam2.SetAttribute("type", "boolean");
            objLoaderParam2.SetAttribute("key", "recomputeVertexNormals");
            objLoaderParam2.SetAttribute("value", "true");

            objLoader.AppendChild(objLoaderParam1);
            objLoader.AppendChild(objLoaderParam2);
            part.AppendChild(objLoader);

            // rotate
            XmlElement rotate = xmlDoc.CreateElement("filter");
            rotate.SetAttribute("type", "rotate");

            XmlElement rotateParam = xmlDoc.CreateElement("param");
            rotateParam.SetAttribute("type", "rotation");
            rotateParam.SetAttribute("key", "rotation");

            XmlElement rotateParamRotRoll = xmlDoc.CreateElement("rot");
            rotateParamRotRoll.SetAttribute("axis", "roll");
            rotateParamRotRoll.SetAttribute("angle_deg", rotation.x.ToString());

            XmlElement rotateParamRotPitch = xmlDoc.CreateElement("rot");
            rotateParamRotPitch.SetAttribute("axis", "pitch");
            rotateParamRotPitch.SetAttribute("angle_deg", rotation.y.ToString());

            XmlElement rotateParamRotYaw = xmlDoc.CreateElement("rot");
            rotateParamRotYaw.SetAttribute("axis", "yaw");
            rotateParamRotYaw.SetAttribute("angle_deg", rotation.z.ToString());

            rotateParam.AppendChild(rotateParamRotRoll);
            rotateParam.AppendChild(rotateParamRotPitch);
            rotateParam.AppendChild(rotateParamRotYaw);
            rotate.AppendChild(rotateParam);
            part.AppendChild(rotate);

            // translate
            XmlElement translate = xmlDoc.CreateElement("filter");
            translate.SetAttribute("type", "translate");

            XmlElement translateParam = xmlDoc.CreateElement("param");
            translateParam.SetAttribute("type", "vec3");
            translateParam.SetAttribute("key", "offset");
            translateParam.SetAttribute("value", string.Format("{0};{1};{2}", position.x, position.y, position.z));

            translate.AppendChild(translateParam);
            part.AppendChild(translate);

            // scale
            XmlElement scale = xmlDoc.CreateElement("filter");
            scale.SetAttribute("type", "scale");

            XmlElement scaleParam = xmlDoc.CreateElement("param");
            scaleParam.SetAttribute("type", "double");
            scaleParam.SetAttribute("key", "scale");
            scaleParam.SetAttribute("value", "1");

            scale.AppendChild(scaleParam);
            part.AppendChild(scale);

            // add part
            root.AppendChild(part);
        }
        xmlDoc.AppendChild(root);
        xmlDoc.Save(Path.Combine(Path.GetDirectoryName(FileNames[0]), filename));
    }

    private void WriteCSV(string filename)
    {
        string outputFileName = Path.Combine(Path.GetDirectoryName(FileNames[0]), filename);
        using (StreamWriter file = new StreamWriter(outputFileName))
        {
            for (int i = 0; i < Stone.childCount; i++)
            {
                var position = Stone.GetChild(i).GetChild(0).position;
                var rotation = Stone.GetChild(i).GetChild(0).eulerAngles;
                file.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                    Path.Combine(Path.GetFileName(Path.GetDirectoryName(PickFileNames[i])), Path.GetFileName(PickFileNames[i])),
                    position.x, position.y, position.z, rotation.x, rotation.y, rotation.z));
            }
        }
    }

    private bool CompareVector(Vector3 v1, Vector3 v2)
    {
        var xdiff = Math.Abs(v1.x - v2.x);
        var ydiff = Math.Abs(v1.y - v2.y);
        var zdiff = Math.Abs(v1.z - v2.z);
        if (xdiff < 1e-3 && ydiff < 1e-3 && zdiff < 1e-3)
            return true;
        return false;
    }
}
