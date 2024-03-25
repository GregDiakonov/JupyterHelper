namespace SeminarHelperServer.Models
{
    public class Student
    { 
        public int Id { get; set; }
        public int SeminarID { get; set; }
        public double Position {  get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
    }
}
