using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class NeonAssetSurvey
{
    public const string Root="Assets/HIVEMIND/CyberpunkCity/URP/Art/Prefabs/";
    public static void RunBatch()
    {
        try
        {
            var report=new List<string>();
            string[] names={"SM_MERGED_BP_Building9","SM_Block_01","SM_Block_02","SM_MERGED_BP_House_Shop_E3","SM_House_Shop_A_Front","SM_House_Shop_B_Front","SM_Building_Wall_C","SM_Balcon_Concrete_Plate_Long","SM_Bridge_A","SM_Rack_Walkway","SM_Stairs-Short","SM_Billboard_Front_Long","SM_Billboard_SIde_Long_01","SM_BigHologram","SM_Lamp_Street","SM_MERGED_Vent_Square_01","SM_Roof_Square","SM_Train_A_Base"};
            foreach(string name in names)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+".prefab");
                if(prefab==null){report.Add("MISSING "+name);continue;}
                var go=UnityEngine.Object.Instantiate(prefab); var renderers=go.GetComponentsInChildren<Renderer>(true);
                Bounds b=default; bool first=true; var shaders=new HashSet<string>(); long vertices=0;
                foreach(var r in renderers){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds); foreach(var m in r.sharedMaterials) shaders.Add(m==null?"NULL":m.name+":"+(m.shader!=null?m.shader.name:"MISSING"));}
                foreach(var f in go.GetComponentsInChildren<MeshFilter>()) if(f.sharedMesh!=null)vertices+=f.sharedMesh.vertexCount;
                report.Add($"{name} SIZE {b.size} CENTER {b.center} RENDERERS {renderers.Length} VERTICES_ALL_LODS {vertices} MATERIALS {string.Join(";",shaders)}");
                UnityEngine.Object.DestroyImmediate(go);
            }
            File.WriteAllLines("Logs/NeonAssetSurvey.txt",report);
            Debug.Log("NEON ASSET SURVEY COMPLETE"); EditorApplication.Exit(0);
        }
        catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
    }
}