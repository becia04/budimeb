using System.Collections.Generic;

namespace budimeb.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }

        public ICollection<Project> Projects { get; set; }
    }
}
