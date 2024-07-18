using System.Collections.Generic;

namespace budimeb.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description {  get; set; }
        public string PhotoPath { get; set; }

        public ICollection<Project> Projects { get; set; }
    }
}
