
namespace PVLib
{
    public struct ChannleList
    {
        public List<string> Channles;
        public int length => Channles.Count;
        string this[int index] => Channles[index];
        public void Add(string channle)
        {
            Channles.Add(channle);
        } 
    }
}
