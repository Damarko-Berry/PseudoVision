using System.Xml.Serialization;


namespace PVLib
{
    public static class SaveLoad<T>
    {
        public static void Save(T obj, string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            var writer = new StreamWriter(path);
            serializer.Serialize(writer, obj);
            writer.Close();
        }
       
        
        public static T Load(string path)
        {
            try
            {
                XmlSerializer serializer = new(typeof(T));
                var reader = new StreamReader(path);
                var obj = (T)serializer.Deserialize(reader);
                reader.Close();
                return obj;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);

            }
        }
    }
}
