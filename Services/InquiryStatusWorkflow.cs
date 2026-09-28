using System;
using System.Collections.Generic;
using System.Linq;

namespace CC.Services
{
    /// <summary>
    /// Centralized, reusable business logic for the customer inquiry status lifecycle:
    /// Lifecycle: New -> In Progress -> Quoted -> Approved -> Converted
    /// - Strictly forward-only progression (no moving backwards).
    /// - Closed is an exit status available from New, In Progress, or Quoted.
    /// - Approved is locked/final (editing disabled, no dropdown changes) except for conversion.
    /// - Converted and Closed are completely terminal/locked.
    /// </summary>
    public static class InquiryStatusWorkflow
    {
        public const string StatusNew = "New";
        public const string StatusInProgress = "In Progress";
        public const string StatusQuoted = "Quoted";
        public const string StatusApproved = "Approved";
        public const string StatusConverted = "Converted";
        public const string StatusClosed = "Closed";

        private static readonly string[] OrderedLifecycle = new[]
        {
            StatusNew,
            StatusInProgress,
            StatusQuoted,
            StatusApproved
        };

        /// <summary>
        /// Checks whether the inquiry status is locked from general editing/status updates.
        /// Approved is locked for conversion only; Converted and Closed are terminal.
        /// </summary>
        public static bool IsLocked(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            return status.Equals(StatusApproved, StringComparison.OrdinalIgnoreCase)
                || status.Equals(StatusConverted, StringComparison.OrdinalIgnoreCase)
                || status.Equals(StatusClosed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks whether an inquiry with this status can be converted into an active Sales Order.
        /// Only "Approved" inquiries are eligible.
        /// </summary>
        public static bool CanConvertToOrder(string? status)
        {
            return string.Equals(status, StatusApproved, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns strictly forward next-step statuses ahead of the current status.
        /// The current status itself and any prior statuses are excluded.
        /// If already Approved, Converted, or Closed, returns empty list.
        /// </summary>
        public static IReadOnlyList<string> GetNextValidStatuses(string? currentStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus))
            {
                return new[] { StatusInProgress, StatusQuoted, StatusApproved, StatusClosed };
            }

            if (IsLocked(currentStatus))
            {
                return Array.Empty<string>();
            }

            int currentIndex = -1;
            for (int i = 0; i < OrderedLifecycle.Length; i++)
            {
                if (OrderedLifecycle[i].Equals(currentStatus, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            var nextStatuses = new List<string>();

            if (currentIndex >= 0)
            {
                for (int i = currentIndex + 1; i < OrderedLifecycle.Length; i++)
                {
                    nextStatuses.Add(OrderedLifecycle[i]);
                }
            }
            else
            {
                nextStatuses.AddRange(new[] { StatusInProgress, StatusQuoted, StatusApproved });
            }

            // Closed is a valid exit status from New, In Progress, and Quoted
            if (!nextStatuses.Contains(StatusClosed, StringComparer.OrdinalIgnoreCase))
            {
                nextStatuses.Add(StatusClosed);
            }

            return nextStatuses;
        }

        /// <summary>
        /// Validates whether a proposed status transition from `fromStatus` to `toStatus` is permitted.
        /// </summary>
        public static bool CanTransition(string? fromStatus, string? toStatus)
        {
            if (string.IsNullOrWhiteSpace(toStatus)) return false;
            if (string.IsNullOrWhiteSpace(fromStatus)) return true;

            // Same status
            if (string.Equals(fromStatus, toStatus, StringComparison.OrdinalIgnoreCase)) return true;

            // Approved -> Converted via Order conversion
            if (string.Equals(fromStatus, StatusApproved, StringComparison.OrdinalIgnoreCase)
                && string.Equals(toStatus, StatusConverted, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var allowed = GetNextValidStatuses(fromStatus);
            return allowed.Any(s => s.Equals(toStatus, StringComparison.OrdinalIgnoreCase));
        }
    }
}
