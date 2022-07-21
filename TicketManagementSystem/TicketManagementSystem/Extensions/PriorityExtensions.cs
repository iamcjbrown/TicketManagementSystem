using System;
namespace TicketManagementSystem
{
    public static class PriorityExtensions
    {
        public static Priority RaisePriority(this Priority priority, bool raisePriority = false)
        {
            if (raisePriority)
            {
                return priority switch
                {
                    Priority.Medium => Priority.High,
                    Priority.Low => Priority.Medium,
                    _ => priority,
                };
            }

            return priority;
        }

        public static double Price(this Priority priority) => priority is Priority.High ? 100 : 50;
    }
}

