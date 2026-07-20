using UnityEditor;
using UnityEngine;
using System.IO;

public static class WhyFileCreator
{
    [MenuItem("Assets/Create/Why Script (.why)")]
    public static void CreateWhyFile()
    {
        string path = GetSelectedPathOrFallback();
        string fileName = "NewScript.why";
        string fullPath = Path.Combine(path, fileName);

        int counter = 1;
        while (File.Exists(fullPath))
        {
            fileName = $"NewScript{counter}.why";
            fullPath = Path.Combine(path, fileName);
            counter++;
        }

        //Overly long string to write a hello world program.
        //For now fully in HEX Bytecode, later I'll change it to assembly-like syntax for readability.
        //Note that this also will be a template file instead of this horrible inline code string lols
        File.WriteAllText(fullPath,
            ".Headers \n" + //Start of headers section
            ".HEX ;This tells the compiler to read the code in hex format \n\n" +
            ";This program emits a simple Hello World program to the console. \n\n" +
            ";Text marked with ; is a comment and will be ignored by the compiler. \n\n" +
            ".Code \n" + //Start of code section
            "0x05, 0x00, 0x48, ;H into register R0 \r\n0x07, 0x00, 0x01, 0x00, 0x00, ;Move to memory address 0 \r\n\n" +
            "0x05, 0x00, 0x65, ;E into register R0 \r\n0x07, 0x00, 0x01, 0x01, 0x00, ;Move to memory address 1 \r\n\n" +
            "0x05, 0x00, 0x6C, ;L into register R0 \r\n0x07, 0x00, 0x01, 0x02, 0x00, ;Move to memory address 2 \r\n\n" +
            "0x05, 0x00, 0x6C, ;L into register R0 \r\n0x07, 0x00, 0x01, 0x03, 0x00, ;Move to memory address 3 \r\n\n" +
            "0x05, 0x00, 0x6F, ;O into register R0 \r\n0x07, 0x00, 0x01, 0x04, 0x00, ;Move to memory address 4 \r\n\n" +
            "0x05, 0x00, 0x20, ;Space into register R0 \r\n0x07, 0x00, 0x01, 0x05, 0x00, ;Move to memory address 5 \r\n\n" +
            "0x05, 0x00, 0x57, ;W into register R0 \r\n0x07, 0x00, 0x01, 0x06, 0x00, ;Move to memory address 6 \r\n\n" +
            "0x05, 0x00, 0x6F, ;O into register R0 \r\n0x07, 0x00, 0x01, 0x07, 0x00, ;Move to memory address 7 \r\n\n" +
            "0x05, 0x00, 0x72, ;R into register R0 \r\n0x07, 0x00, 0x01, 0x08, 0x00, ;Move to memory address 8 \r\n\n" +
            "0x05, 0x00, 0x6C, ;L into register R0 \r\n0x07, 0x00, 0x01, 0x09, 0x00, ;Move to memory address 9 \r\n\n" +
            "0x05, 0x00, 0x64, ;D into register R0 \r\n0x07, 0x00, 0x01, 0x0A, 0x00, ;Move to memory address 10 \r\n\n" +
            "0x05, 0x00, 0x00, ;Null terminator into register R0 \r\n0x07, 0x00, 0x01, 0x0B, 0x00, ;Move to memory address 11 \r\n\n" +
            "0x05, 0x0F, 0x00, ;Output type: String \r\n" +
            "0x05, 0x10, 0x01, ;Source type: Memory \r\n" +
            "0x05, 0x11, 0x00, ;Source: Memory address 0 \r\n" +
            "0x02, 0x01, ;CoreCall: SysWrite \r\n" +
            "0x00 ;Exit the program");

        AssetDatabase.Refresh();

        Object obj = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
        Selection.activeObject = obj;
    }

    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";

        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            string selectedPath = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(selectedPath))
                path = selectedPath;
            else
                path = Path.GetDirectoryName(selectedPath);
        }

        return path;
    }
}