using System;
using System.Collections.Generic;
using System.Linq;

namespace CC.Services
{
    /// <summary>
    /// Centralized, reusable business logic for the sales order status lifecycle:
    /// Lifecycle: Pending (0) -> Confirmed (1) -> Processing (2) -> Ready (4) -> Completed (3)
    /// - Strictly forward-only progression (no moving backwards).
    /// - Cancelled (5) is an exit status available from any active state before completion.
    /// - Completed (3) and Cancelled (5) are terminal/locked.
    /// </summary>
    public static class OrderStatusWorkflow
    {
        public const int StatusPending = 0;
        public const int StatusConfirmed = 1;
        public const int StatusProcessing = 2;
        public const int StatusCompleted = 3;
        public const int StatusReady = 4;
        public const int StatusCancelled = 5;

        // Sequence of progressive stages
        private static readonly int[] OrderedLifecycle = new[]
        {
            StatusPending,     // 0
            StatusConfirmed,   // 1
            StatusProcessing,  // 2
            StatusReady,       // 4
            StatusCompleted    // 3
        };

        public static string GetStatusName(int statusId)
        {
            return statusId switch
            {
                StatusPending => "Pending",
                StatusConfirmed => "Confirmed",
                StatusProcessing => "Processing",
                StatusReady => "Ready",
                StatusCompleted => "Completed",
                StatusCancelled => "Cancelled",
                _ => "Unknown"
            };
        }

        public static int GetStatusId(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName)) return StatusPending;
            return statusName.Trim().ToLowerInvariant() switch
            {
                "pending" => StatusPending,
                "confirmed" => StatusConfirmed,
                "processing" => StatusProcessing,
                "ready" => StatusReady,
                "completed" => StatusCompleted,
                "cancelled" => StatusCancelled,
                _ => StatusPending
            };
        }

        /// <summary>
        /// Returns the 0-based stepper index (0: Pending, 1: Confirmed, 2: Processing, 3: Ready, 4: Completed).
        /// Returns -1 if Cancelled or Unknown.
        /// </summary>
        public static int GetStepperStepIndex(int statusId)
        {
            return statusId switch
            {
                StatusPending => 0,
                StatusConfirmed => 1,
                StatusProcessing => 2,
                StatusReady => 3,
                StatusCompleted => 4,
                _ => -1
            };
        }

        /// <summary>
        /// Checks whether the order status is locked from further updates.
        /// Completed and Cancelled are terminal.
        /// </summary>
        public static bool IsLocked(int statusId)
        {
            return statusId == StatusCompleted || statusId == StatusCancelled;
        }

        /// <summary>
        /// Returns strictly forward next-step status IDs ahead of currentStatusId in the sequence, plus Cancelled.
        /// Current status itself and any prior statuses are excluded.
        /// If already Completed or Cancelled, returns empty list.
        /// </summary>
        public static IReadOnlyList<int> GetNextValidStatusIds(int currentStatusId)
        {
            if (IsLocked(currentStatusId))
            {
                return Array.Empty<int>();
            }

            int currentIndex = Array.IndexOf(OrderedLifecycle, currentStatusId);
            if (currentIndex < 0)
            {
                return new[] { StatusConfirmed, StatusProcessing, StatusReady, StatusCompleted, StatusCancelled };
            }

            var nextStatuses = new List<int>();

            // Only statuses strictly after current step
            for (int i = currentIndex + 1; i < OrderedLifecycle.Length; i++)
            {
                nextStatuses.Add(OrderedLifecycle[i]);
            }

            // Cancelled is always an exit option from any non-terminal state
            if (!nextStatuses.Contains(StatusCancelled))
            {
                nextStatuses.Add(StatusCancelled);
            }

            return nextStatuses;
        }

        /// <summary>
        /// Returns status names for valid next-step transitions.
        /// </summary>
        public static IReadOnlyList<string> GetNextValidStatusNames(int currentStatusId)
        {
            return GetNextValidStatusIds(currentStatusId).Select(GetStatusName).ToList();
        }

        /// <summary>
        /// Validates whether transitioning from fromStatusId to toStatusId is permitted.
        /// Returns true if no change (same status).
        /// </summary>
        public static bool CanTransition(int fromStatusId, int toStatusId)
        {
            if (fromStatusId == toStatusId) return true;
            if (IsLocked(fromStatusId)) return false;

            return GetNextValidStatusIds(fromStatusId).Contains(toStatusId);
        }
    }
}
