using System;
using System.Collections.Generic;

namespace Darwin.Application.Loyalty.DTOs
{
    public sealed class MyPromotionsDto
    {
        public Guid? BusinessId { get; init; }
        public int MaxItems { get; init; } = 20;
    }

    public sealed class PromotionFeedItemDto
    {
        public Guid BusinessId { get; init; }
        public string BusinessName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string CtaKind { get; init; } = "OpenRewards";
        public int Priority { get; init; }
    }

    public sealed class MyPromotionsResultDto
    {
        public List<PromotionFeedItemDto> Items { get; init; } = new();
    }
}