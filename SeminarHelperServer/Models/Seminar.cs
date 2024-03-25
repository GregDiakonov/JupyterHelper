namespace SeminarHelperServer.Models
{
    public class Seminar
    {
        public int Id { get; set; }
        public int textboxes { get; set; }
        public DateTime created { get; set; }
        
        public List<Student> students { get; set; }
    }
}
