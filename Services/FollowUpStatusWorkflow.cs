using System;
using System.Collections.Generic;
using System.Linq;

namespace CC.Services
{
    /// <summary>
    /// Centralized, reusable business logic for the follow-up status lifecycle:
    /// Statuses: Pending (0), Completed (1), Cancelled (2), Overdue (3)
    /// - Strictly forward-only progression (no moving backwards).
    /// - Pending or Overdue can transition to Completed or Cancelled.
    /// - Completed and Cancelled are terminal/locked.
    /// </summary>
    public static class FollowUpStatusWorkflow
    {
        public const int StatusPending = 0;
        public const int StatusCompleted = 1;
        public const int StatusCancelled = 2;
        public const int StatusOverdue = 3;

        public static string GetStatusName(int statusId)
        {
            return statusId switch
            {
                StatusPending => "Pending",
                StatusCompleted => "Completed",
                StatusCancelled => "Cancelled",
                StatusOverdue => "Overdue",
                _ => "Unknown"
            };
        }

        public static int GetStatusId(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName)) return StatusPending;
            return statusName.Trim().ToLowerInvariant() switch
            {
                "pending" => StatusPending,
                "completed" => StatusCompleted,
                "cancelled" => StatusCancelled,
                "overdue" => StatusOverdue,
                _ => StatusPending
            };
        }

        public static bool IsLocked(int statusId)
        {
            return statusId == StatusCompleted || statusId == StatusCancelled;
        }

        public static IReadOnlyList<int> GetNextValidStatusIds(int currentStatusId)
        {
            return currentStatusId switch
            {
                StatusPending => new[] { StatusCompleted, StatusCancelled },
                StatusOverdue => new[] { StatusCompleted, StatusCancelled },
                StatusCompleted => Array.Empty<int>(),
                StatusCancelled => Array.Empty<int>(),
                _ => new[] { StatusCompleted, StatusCancelled }
            };
        }

        public static IReadOnlyList<string> GetNextValidStatusNames(int currentStatusId)
        {
            return GetNextValidStatusIds(currentStatusId).Select(GetStatusName).ToList();
        }

        public static bool CanTransition(int fromStatusId, int toStatusId)
        {
            if (fromStatusId == toStatusId) return true;
            if (IsLocked(fromStatusId)) return false;

            return GetNextValidStatusIds(fromStatusId).Contains(toStatusId);
        }
    }
}
