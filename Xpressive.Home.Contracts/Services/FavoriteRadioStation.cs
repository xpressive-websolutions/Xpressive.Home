using NPoco;

namespace Xpressive.Home.Contracts.Services
{
    [TableName("Radio")]
    [PrimaryKey("Id", AutoIncrement = false)]
    public class FavoriteRadioStation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
    }
}
