namespace Simple.OData.Client.Tests
{
    public class Activity
    {
        public long Id { get; set; }

        public string Number { get; set; }

        public string Title { get; set; }

        public Option Option { get; set; }

        public Ticket Ticket { get; set; }
    }

    public class Ticket
    {
        public long Id { get; set; }

        public string Number { get; set; }

        public string Title { get; set; }
    }

    public class Option
    {
        public long Id { get; set; }

        public string Description { get; set; }
    }
}