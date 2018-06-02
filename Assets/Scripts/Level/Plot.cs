﻿using Assets.Scripts.Level;
using Assets.Scripts.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PlotType { Grass, Water, Sand, Stone }

public class Plot : MonoBehaviour
{
    [Header("Optimization")]

    [SerializeField] private GameObject Hexagon;
    private bool IsSolid { get; set; }

    [Header("Ground Layers")]
    [SerializeField] private Transform SecondaryLayer;
    [SerializeField] private Transform PrimaryLayer;
    [SerializeField] private float DirtPercentage;

    [Header("Top Layers")]
    [SerializeField] private Transform TopPosition;
    [SerializeField] private GameObject GrassTop;
    [SerializeField] private GameObject WaterTop;
    [SerializeField] private float WaterMaxDiscoloring;
    [SerializeField] private float WaterMaxDiscolorDepth;
    [SerializeField] private GameObject SandTop;
    [SerializeField] private GameObject StoneTop;
    private GameObject Top;

    [Header("Materials")]
    [SerializeField] private Material DirtMaterial;
    [SerializeField] private Material StoneMaterial;
    [SerializeField] private Material SandMaterial;

    [Header("Weather")]
    [SerializeField] private float SnowTemp;
    [SerializeField] private float StoneTemp;
    [SerializeField] private float GrassTemp;
    [SerializeField] private GameObject SnowLayer;
    private GameObject Layer;

    [Header("Positions")]
    [SerializeField] private Transform BuildingPosition;
    [SerializeField] private Transform ResourcesPosition;

    [Header("Resources")]
    [SerializeField] private GameObject TreePrefab;
    [SerializeField] private float MinScaleTree;
    [SerializeField] private float MaxScaleTree;

    private List<GameObject> resources = new List<GameObject>();

    private ObjectFade[] FadeObject;

    private PlotType type = PlotType.Grass;
    public PlotType Type
    {
        get
        {
            return type;
        }
        set
        {
            type = value;
            UpdatePlot();

        }
    }

    public HexLocation Location { get; set; }
    private float height;
    public float Height
    {
        get
        {
            return height;
        }
        set
        {
            this.height = value;
            UpdatePlot();
        }
    }

    private float temperature;
    public float Temperature
    {
        get
        {
            return temperature;
        }
        set
        {
            temperature = value;

            if (Type != PlotType.Water)
            {
                if (temperature <= SnowTemp)
                {
                    if (Layer != null)
                    {
                        GameObject.Destroy(Layer);
                    }
                    Layer = GameObject.Instantiate(SnowLayer);
                    Layer.transform.parent = TopPosition;
                    Layer.transform.localPosition = new Vector3();
                }

                if (temperature <= StoneTemp)
                {
                    Type = PlotType.Stone;
                }

                if (Type == PlotType.Grass)
                {
                    MeshRenderer renderer = Top.transform.Find("Hexagon_Grass_Top").GetComponent<MeshRenderer>();
                    float alpha = (temperature + GrassTemp) / (Grid.Instance.baseTemperature);
                    renderer.material.color *= alpha;
                }
            }

        }
    }

    public void SetTrees(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject tree = GameObject.Instantiate(TreePrefab);
            float x = (float)NoiseMapGenerator.Random.NextDouble() * 4;
            float z = (float)NoiseMapGenerator.Random.NextDouble() * 4;
            float scale = MinScaleTree + (float)(NoiseMapGenerator.Random.NextDouble() * (MaxScaleTree - MinScaleTree));
            tree.transform.parent = ResourcesPosition;
            tree.transform.localPosition = new Vector3(x, 0, z);
            tree.transform.localScale = new Vector3(scale, scale, scale);
            tree.transform.localRotation = Quaternion.Euler(0, (float)(NoiseMapGenerator.Random.NextDouble() * 360.0), 0);
        }
    }

    public void SetBuilding(GameObject obj, int rotation)
    {
        obj.transform.parent = BuildingPosition;
        obj.transform.localPosition = new Vector3();
        obj.transform.localRotation = Quaternion.Euler(0, 30 * rotation, 0);
    }

    // Use this for initialization
    void Start()
    {
        FadeObject = GetComponentsInChildren<ObjectFade>();
    }

    public void UpdatePlot()
    {
        //Scale plot
        float currentHeight = this.transform.position.y;
        this.transform.Translate(new Vector3(0, (Height - currentHeight), 0), Space.Self);
        SecondaryLayer.localScale = new Vector3(1, Height * (1 - DirtPercentage), 1);
        SecondaryLayer.localPosition = new Vector3(0, -Height * DirtPercentage - 1, 0);
        PrimaryLayer.localScale = new Vector3(1, Height * DirtPercentage, 1);

        //Set top
        switch (type)
        {
            case PlotType.Grass:
                if (Top != null)
                {
                    GameObject.Destroy(Top);
                }
                Top = GameObject.Instantiate(GrassTop);
                Top.transform.parent = TopPosition;
                Top.transform.localPosition = new Vector3();
                PrimaryLayer.GetComponent<MeshRenderer>().material = DirtMaterial;
                break;
            case PlotType.Water:
                if (Top != null)
                {
                    GameObject.Destroy(Top);
                }
                Top = GameObject.Instantiate(WaterTop);
                Top.transform.parent = TopPosition;
                Top.transform.localPosition = new Vector3(0, -(Height - Grid.Instance.SeaLevel), 0);
                PrimaryLayer.GetComponent<MeshRenderer>().material = DirtMaterial;

                MeshRenderer renderer = Top.transform.Find("Water").GetComponent<MeshRenderer>();
                float alpha = Mathf.Min((height) / (Grid.Instance.SeaLevel- WaterMaxDiscolorDepth) ,1);
                renderer.material.color *= Mathf.Max(alpha, WaterMaxDiscoloring);

                break;
            case PlotType.Sand:
                if (Top != null)
                {
                    GameObject.Destroy(Top);
                }
                Top = GameObject.Instantiate(SandTop);
                Top.transform.parent = TopPosition;
                Top.transform.localPosition = new Vector3();
                PrimaryLayer.GetComponent<MeshRenderer>().material = SandMaterial;
                break;
            case PlotType.Stone:
                if (Top != null)
                {
                    GameObject.Destroy(Top);
                }
                Top = GameObject.Instantiate(StoneTop);
                Top.transform.parent = TopPosition;
                Top.transform.localPosition = new Vector3();
                PrimaryLayer.GetComponent<MeshRenderer>().material = StoneMaterial;
                break;
        }
    }

    public void ToggleHex(bool enable)
    {

        // Debug.Log(enable);
        if (Hexagon.activeSelf != enable)
        {
            Hexagon.SetActive(enable);
        }
        if (ResourcesPosition.gameObject.activeSelf != enable)
        {
            ResourcesPosition.gameObject.SetActive(enable);
        }
    }

    public void UpdateFade(float fade)
    {
        if (fade == 1 && IsSolid) return;
        if (fade == 1) IsSolid = true;
        if (fade < 1) IsSolid = false;

        foreach (ObjectFade objectFade in FadeObject)
        {
            objectFade.SetFade(fade);
        }
    }
}
