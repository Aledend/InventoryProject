using System.Collections.Generic;
public static class Extensions
{
    public static string Repeat(this string s, int repeat)
    {
        string repeated = string.Empty;
        for(int i = 0; i < repeat; i++)
        {
            repeated += s;
        }
        return repeated;
    }

    public static string ToCategoryString(this CategoryName category)
    {
        string name = System.Enum.GetName(typeof(CategoryName), category);
        return string.IsNullOrEmpty(name) ? "Fetching." : name;
    }

    public static string ToCategoryPath(this CategoryName category, CategorySystem.CategoryAPI m_ItemManager)
    {
        Queue<string> builtString = new Queue<string>();

        CategoryName currentCategory = category;

        builtString.Enqueue(ToCategoryString(currentCategory));

        while (m_ItemManager.FetchCategoryRef((int)currentCategory).ParentCategory != currentCategory) 
        {
            currentCategory = m_ItemManager.FetchCategoryRef((int)currentCategory).ParentCategory;

            builtString.Enqueue(ToCategoryString(currentCategory));
        }

        return string.Join("/", builtString.ToArray());
    }
}
