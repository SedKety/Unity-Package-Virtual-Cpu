using UnityEditor;
using UnityEngine;
using System.IO;

public static class WhyFileCreator
{
    [MenuItem("Assets/Create/Why Script/Empty")]
    public static void CreateEmpty() => CreateFile("NewScript", TemplateEmpty);

    [MenuItem("Assets/Create/Why Script/HEX")]
    public static void CreateHex() => CreateFile("NewScript", TemplateHex);

    [MenuItem("Assets/Create/Why Script/ASM")]
    public static void CreateAsm() => CreateFile("NewScript", TemplateAsm);

    [MenuItem("Assets/Create/Why Script/DEC")]
    public static void CreateDec() => CreateFile("NewScript", TemplateDec);

    [MenuItem("Assets/Create/Why Script/BIN")]
    public static void CreateBin() => CreateFile("NewScript", TemplateBin);

    /// <summary>
    /// Creates the selected file in the given directory
    /// </summary>
    /// <param name="baseName"></param>
    /// <param name="content"></param>
    private static void CreateFile(string baseName, string content)
    {
        string dir = GetSelectedPathOrFallback();
        string fileName = baseName + ".why";
        string fullPath = Path.Combine(dir, fileName);

        int counter = 1;
        while (File.Exists(fullPath))
        {
            fileName = $"{baseName}{counter}.why";
            fullPath = Path.Combine(dir, fileName);
            counter++;
        }

        File.WriteAllText(fullPath, content);
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
    }

    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            string selected = AssetDatabase.GetAssetPath(obj);
            path = Directory.Exists(selected) ? selected : Path.GetDirectoryName(selected);
        }
        return path;
    }

    public const string TemplateEmpty =
        ".Headers\n" +
        "; script settings go here\n" +
        "\n" +
        ".Code\n" +
        "; code goes here\n";

    public const string TemplateHex =
        ".Headers\n" +
        "    #HEX\n" +
        "    #MEMSIZE 16\n" +
        "    #STACKSIZE 8\n" +
        "    #LOOP 0\n" +
        "    #TICK_RATE 10\n" +
        "\n" +
        ".Code\n" +
        "    0x05, 0x00, 0x48, ;H\n" +
        "    0x07, 0x00, 0x01, 0x00, 0x00, ;MOV R0 → mem[0]\n" +
        "    0x05, 0x00, 0x65, ;e\n" +
        "    0x07, 0x00, 0x01, 0x01, 0x00, ;MOV R0 → mem[1]\n" +
        "    0x05, 0x00, 0x6C, ;l\n" +
        "    0x07, 0x00, 0x01, 0x02, 0x00, ;MOV R0 → mem[2]\n" +
        "    0x05, 0x00, 0x6C, ;l\n" +
        "    0x07, 0x00, 0x01, 0x03, 0x00, ;MOV R0 → mem[3]\n" +
        "    0x05, 0x00, 0x6F, ;o\n" +
        "    0x07, 0x00, 0x01, 0x04, 0x00, ;MOV R0 → mem[4]\n" +
        "    0x05, 0x00, 0x20, ;space\n" +
        "    0x07, 0x00, 0x01, 0x05, 0x00, ;MOV R0 → mem[5]\n" +
        "    0x05, 0x00, 0x57, ;W\n" +
        "    0x07, 0x00, 0x01, 0x06, 0x00, ;MOV R0 → mem[6]\n" +
        "    0x05, 0x00, 0x6F, ;o\n" +
        "    0x07, 0x00, 0x01, 0x07, 0x00, ;MOV R0 → mem[7]\n" +
        "    0x05, 0x00, 0x72, ;r\n" +
        "    0x07, 0x00, 0x01, 0x08, 0x00, ;MOV R0 → mem[8]\n" +
        "    0x05, 0x00, 0x6C, ;l\n" +
        "    0x07, 0x00, 0x01, 0x09, 0x00, ;MOV R0 → mem[9]\n" +
        "    0x05, 0x00, 0x64, ;d\n" +
        "    0x07, 0x00, 0x01, 0x0A, 0x00, ;MOV R0 → mem[10]\n" +
        "    0x05, 0x00, 0x00, ;null\n" +
        "    0x07, 0x00, 0x01, 0x0B, 0x00, ;MOV R0 → mem[11]\n" +
        "    0x05, 0x0F, 0x00, ;ECX = String output\n" +
        "    0x05, 0x10, 0x01, ;EDX = Memory source\n" +
        "    0x05, 0x11, 0x00, ;ESI = address 0\n" +
        "    0x02, 0x01, ;CORECALL SysWrite\n" +
        "    0x00 ;END\n";

    public const string TemplateAsm =
        ".Headers\n" +
        "    #ASM\n" +
        "    #MEMSIZE 16\n" +
        "    #STACKSIZE 8\n" +
        "    #LOOP 0\n" +
        "    #TICK_RATE 10\n" +
        "\n" +
        ".Code\n" +
        "    LOAD R0, 72         ; 'H' → mem[0]\n" +
        "    MOV R0, 0\n" +
        "    LOAD R0, 101        ; 'e' → mem[1]\n" +
        "    MOV R0, 1\n" +
        "    LOAD R0, 108        ; 'l' → mem[2]\n" +
        "    MOV R0, 2\n" +
        "    LOAD R0, 108        ; 'l' → mem[3]\n" +
        "    MOV R0, 3\n" +
        "    LOAD R0, 111        ; 'o' → mem[4]\n" +
        "    MOV R0, 4\n" +
        "    LOAD R0, 32         ; ' ' → mem[5]\n" +
        "    MOV R0, 5\n" +
        "    LOAD R0, 87         ; 'W' → mem[6]\n" +
        "    MOV R0, 6\n" +
        "    LOAD R0, 111        ; 'o' → mem[7]\n" +
        "    MOV R0, 7\n" +
        "    LOAD R0, 114        ; 'r' → mem[8]\n" +
        "    MOV R0, 8\n" +
        "    LOAD R0, 108        ; 'l' → mem[9]\n" +
        "    MOV R0, 9\n" +
        "    LOAD R0, 100        ; 'd' → mem[10]\n" +
        "    MOV R0, 10\n" +
        "    LOAD R0, 0          ; null → mem[11]\n" +
        "    MOV R0, 11\n" +
        "    LOAD ECX, 0         ; Output type: String\n" +
        "    LOAD EDX, 1         ; Source type: Memory\n" +
        "    LOAD ESI, 0         ; Source: address 0\n" +
        "    CORECALL 1          ; SysWrite\n" +
        "    END\n";

    public const string TemplateDec =
        ".Headers\n" +
        "    #DEC\n" +
        "    #MEMSIZE 16\n" +
        "    #STACKSIZE 8\n" +
        "    #LOOP 0\n" +
        "    #TICK_RATE 10\n" +
        "\n" +
        ".Code\n" +
        "    5 0 72          ;LOAD R0 'H'\n" +
        "    7 0 1 0 0       ;MOV R0 → mem[0]\n" +
        "    5 0 101         ;LOAD R0 'e'\n" +
        "    7 0 1 1 0       ;MOV R0 → mem[1]\n" +
        "    5 0 108         ;LOAD R0 'l'\n" +
        "    7 0 1 2 0       ;MOV R0 → mem[2]\n" +
        "    5 0 108         ;LOAD R0 'l'\n" +
        "    7 0 1 3 0       ;MOV R0 → mem[3]\n" +
        "    5 0 111         ;LOAD R0 'o'\n" +
        "    7 0 1 4 0       ;MOV R0 → mem[4]\n" +
        "    5 0 32          ;LOAD R0 ' '\n" +
        "    7 0 1 5 0       ;MOV R0 → mem[5]\n" +
        "    5 0 87          ;LOAD R0 'W'\n" +
        "    7 0 1 6 0       ;MOV R0 → mem[6]\n" +
        "    5 0 111         ;LOAD R0 'o'\n" +
        "    7 0 1 7 0       ;MOV R0 → mem[7]\n" +
        "    5 0 114         ;LOAD R0 'r'\n" +
        "    7 0 1 8 0       ;MOV R0 → mem[8]\n" +
        "    5 0 108         ;LOAD R0 'l'\n" +
        "    7 0 1 9 0       ;MOV R0 → mem[9]\n" +
        "    5 0 100         ;LOAD R0 'd'\n" +
        "    7 0 1 10 0      ;MOV R0 → mem[10]\n" +
        "    5 0 0           ;LOAD R0 null\n" +
        "    7 0 1 11 0      ;MOV R0 → mem[11]\n" +
        "    5 15 0          ;LOAD ECX String output\n" +
        "    5 16 1          ;LOAD EDX Memory source\n" +
        "    5 17 0          ;LOAD ESI address 0\n" +
        "    2 1             ;CORECALL SysWrite\n" +
        "    0               ;END\n";

    public const string TemplateBin =
        ".Headers\n" +
        "    #BIN\n" +
        "    #MEMSIZE 16\n" +
        "    #STACKSIZE 8\n" +
        "    #LOOP 0\n" +
        "    #TICK_RATE 10\n" +
        "\n" +
        "    ;Binary format: every value is a binary integer (e.g. 00000101 = 5 = LOAD)\n" +
        "    ;Spaces and commas are both valid separators\n" +
        "\n" +
        ".Code\n" +
        "    00000101 00000000 01001000  ;LOAD R0 'H'\n" +
        "    00000111 00000000 00000001 00000000 00000000  ;MOV R0 → mem[0]\n" +
        "    00000101 00000000 01100101  ;LOAD R0 'e'\n" +
        "    00000111 00000000 00000001 00000001 00000000  ;MOV R0 → mem[1]\n" +
        "    00000101 00000000 01101100  ;LOAD R0 'l'\n" +
        "    00000111 00000000 00000001 00000010 00000000  ;MOV R0 → mem[2]\n" +
        "    00000101 00000000 01101100  ;LOAD R0 'l'\n" +
        "    00000111 00000000 00000001 00000011 00000000  ;MOV R0 → mem[3]\n" +
        "    00000101 00000000 01101111  ;LOAD R0 'o'\n" +
        "    00000111 00000000 00000001 00000100 00000000  ;MOV R0 → mem[4]\n" +
        "    00000101 00000000 00100000  ;LOAD R0 ' '\n" +
        "    00000111 00000000 00000001 00000101 00000000  ;MOV R0 → mem[5]\n" +
        "    00000101 00000000 01010111  ;LOAD R0 'W'\n" +
        "    00000111 00000000 00000001 00000110 00000000  ;MOV R0 → mem[6]\n" +
        "    00000101 00000000 01101111  ;LOAD R0 'o'\n" +
        "    00000111 00000000 00000001 00000111 00000000  ;MOV R0 → mem[7]\n" +
        "    00000101 00000000 01110010  ;LOAD R0 'r'\n" +
        "    00000111 00000000 00000001 00001000 00000000  ;MOV R0 → mem[8]\n" +
        "    00000101 00000000 01101100  ;LOAD R0 'l'\n" +
        "    00000111 00000000 00000001 00001001 00000000  ;MOV R0 → mem[9]\n" +
        "    00000101 00000000 01100100  ;LOAD R0 'd'\n" +
        "    00000111 00000000 00000001 00001010 00000000  ;MOV R0 → mem[10]\n" +
        "    00000101 00000000 00000000  ;LOAD R0 null\n" +
        "    00000111 00000000 00000001 00001011 00000000  ;MOV R0 → mem[11]\n" +
        "    00000101 00001111 00000000  ;LOAD ECX String output\n" +
        "    00000101 00010000 00000001  ;LOAD EDX Memory source\n" +
        "    00000101 00010001 00000000  ;LOAD ESI address 0\n" +
        "    00000010 00000001           ;CORECALL SysWrite\n" +
        "    00000000                    ;END\n";
}
