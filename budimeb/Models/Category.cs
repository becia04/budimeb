using System.Collections.Generic;

namespace budimeb.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Project> Projects { get; set; }
    }
}
