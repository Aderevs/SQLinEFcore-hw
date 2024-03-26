using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLinEFcore_hw
{
    public class Word
    {
        public Guid Id { get; set; }
        public string? Header { get; set; }
        public string KeyWord { get; set; }
        public List<KeyParams> PrpductLink { get; set; }
    }
}
