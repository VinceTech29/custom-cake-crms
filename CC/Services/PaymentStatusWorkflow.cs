using System;
using System.Collections.Generic;
using System.Linq;

namespace CC.Services
{
    /// <summary>
    /// Centralized, reusable business logic for the payment status lifecycle:
    /// Statuses: Pending (0), Completed (1), Failed (2), Refunded (3)
    /// - Strictly forward-only progression (no moving backwards).
    /// - Pending can transition to Completed or Failed.
    /// - Completed can transition to Refunded.
    /// - Failed and Refunded are terminal/locked.
    /// </summary>
    public static class PaymentStatusWorkflow
    {
        public const int StatusPending = 0;
        public const int StatusCompleted = 1;
        public const int StatusFailed = 2;
        public const int StatusRefunded = 3;

        public static string GetStatusName(int statusId)
        {
            return statusId switch
            {
                StatusPending => "Pending",
                StatusCompleted => "Completed",
                StatusFailed => "Failed",
                StatusRefunded => "Refunded",
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
                "failed" => StatusFailed,
                "refunded" => StatusRefunded,
                _ => StatusPending
            };
        }

        public static bool IsLocked(int statusId)
        {
            return statusId == StatusFailed || statusId == StatusRefunded;
        }

        public static IReadOnlyList<int> GetNextValidStatusIds(int currentStatusId)
        {
            return currentStatusId switch
            {
                StatusPending => new[] { StatusCompleted, StatusFailed },
                StatusCompleted => new[] { StatusRefunded },
                StatusFailed => Array.Empty<int>(),
                StatusRefunded => Array.Empty<int>(),
                _ => new[] { StatusCompleted, StatusFailed }
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
