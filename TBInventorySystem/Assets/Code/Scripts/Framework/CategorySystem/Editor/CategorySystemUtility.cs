#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace CategorySystem
{
    public class CategorySystemUtility
    {
        public static void AddCategoryEnumToFile(string name, int index, ScriptableObject categoryAPI)
        {
            string enumPath = FetchEnumPath(categoryAPI);
            int offset = 2;

            #region MakeSureFileExists
            if (!File.Exists(enumPath))
            {
                StreamWriter fileCreate = File.CreateText(enumPath);
                fileCreate.WriteLine("public enum ECategory" +
                    "{" +
                    "CATEGORY_MAX" +
                    "}");
                fileCreate.Close();
            }
            #endregion // MakeSureFileExists

            string[] lines = File.ReadAllLines(enumPath);

            foreach(string line in lines)
            {
                if(line.Trim() == name)
                {
                    Debug.LogError("Tried adding an already existing enum.");
                    return; 
                }
            }

            Assert.IsTrue(lines.Length >= 4, "File contains too few lines to be valid.");

            StreamWriter fileWrite = new StreamWriter(enumPath);
            for(int i = 0; i < index + offset; i++)
            {
                fileWrite.WriteLine(lines[i]);
            }
            fileWrite.WriteLine("    " + name + ',');
            for(int i = index + offset; i < lines.Length; i++)
            {
                fileWrite.WriteLine(lines[i]);
            }
            fileWrite.Close();
        }

        public static void RemoveCategoryEnumFromFile(int index, ScriptableObject categoryAPI)
        {
            string enumPath = FetchEnumPath(categoryAPI);
            int offset = 2;

            #region MakeSureFileExists
            if (!File.Exists(enumPath))
            {
                StreamWriter fileCreate = File.CreateText(enumPath);
                fileCreate.WriteLine("public enum ECategory" +
                    "{" +
                    "CATEGORY_MAX" +
                    "}");
                fileCreate.Close();
            }
            #endregion // MakeSureFileExists

            string[] lines = File.ReadAllLines(enumPath);

            Assert.IsTrue(lines.Length >= 4, "File contains too few lines to be valid.");

            StreamWriter fileWrite = new StreamWriter(enumPath);
            int ignore = offset + index;
            for(int i = 0; i < lines.Length; i++)
            {
                if(i != ignore)
                    fileWrite.WriteLine(lines[i]);
            }
            fileWrite.Close();
        }

        public static string FetchEnumPath(ScriptableObject categoryAPI)
        {
            string dataPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            string assetPath = AssetDatabase.GetAssetPath(categoryAPI);

            List<string> assetPathParts = new List<string>(assetPath.Split('/'));
            assetPathParts.RemoveAt(assetPathParts.Count - 1);
            assetPath = string.Join("/", assetPathParts.ToArray());

            return dataPath + assetPath + "/Data/CategoryName.cs";
        }
    }
}

#endif // UNITY_EDITOR