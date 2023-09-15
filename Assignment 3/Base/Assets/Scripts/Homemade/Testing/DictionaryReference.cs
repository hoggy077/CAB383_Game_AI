using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DictionaryReference : MonoBehaviour
{
    public List<testInstance> testItems = new List<testInstance>();
    public List<testInstance> testItems_obj = new List<testInstance>();

    public void test()
    {
        if (testItems[0] == null)
        {
            Debug.Log("Nulled");
            return;
        }
        testItems[0].call();
    }

    public void testObj()
    {
        if (testItems_obj[0] == null)
        {
            Debug.Log("Nulled");
            return;
        }
        Debug.Log($"{testItems_obj[0].name}");
    }
}

[CustomEditor(typeof(DictionaryReference))]
class DictionaryReference_Editor : Editor{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("run test"))
            (target as DictionaryReference).test();
        if (GUILayout.Button("run test obj"))
            (target as DictionaryReference).testObj();
    }
}
