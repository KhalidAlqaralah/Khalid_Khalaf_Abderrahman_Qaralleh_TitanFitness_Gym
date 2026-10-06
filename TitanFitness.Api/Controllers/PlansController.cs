using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TitanFitness.Api.Auth;
using TitanFitness.Api.Common;
using TitanFitness.Application.Common;
using TitanFitness.Application.Features.Plans.Commands.CreatePlan;
using TitanFitness.Application.Features.Plans.Commands.UpdatePlan;
using TitanFitness.Application.Features.Plans.Contracts;
using TitanFitness.Application.Features.Plans.Queries.GetPlanById;
using TitanFitness.Application.Features.Plans.Queries.GetPlanFilterOptions;
using TitanFitness.Application.Features.Plans.Queries.GetPlanLookup;
using TitanFitness.Application.Features.Plans.Queries.GetPlans;
using TitanFitness.Domain.Memberships;

namespace TitanFitness.Api.Controllers;

[ApiController]
[Route("api/plans")]
[Authorize(Policy = Policies.Staff)]
public sealed class PlansController(ISender sender) : ControllerBase
{
    /// <summary>Plan Catalogue: paged, sortable, filterable by duration, access, price range and status.</summary>
    [HttpGet]
    [Authorize(Policy = Policies.Manager)]
    public Task<PagedResult<PlanResponse>> GetPlans([FromQuery] GetPlansRequest request, CancellationToken cancellationToken)
    {
        var statuses = request.Statuses ?? [];
        bool? published = statuses.Distinct().Count() == 1 ? statuses[0] == "Published" : null;

        return sender.Send(new GetPlansQuery(request.Page, request.PageSize, request.Search, request.SortBy, request.SortDirection,
            request.Durations ?? [], request.Access, request.MinPrice, request.MaxPrice, published), cancellationToken);
    }

    /// <summary>Distinct durations and price bounds for the Filter Plans dialog.</summary>
    [HttpGet("filter-options")]
    [Authorize(Policy = Policies.Manager)]
    public Task<PlanFilterOptionsResponse> GetFilterOptions(CancellationToken cancellationToken) =>
        sender.Send(new GetPlanFilterOptionsQuery(), cancellationToken);

    /// <summary>Published plans for the Sell Plan dialog on the member profile (front desk can use it).</summary>
    [HttpGet("lookup")]
    public Task<IReadOnlyList<PlanResponse>> GetLookup(CancellationToken cancellationToken) =>
        sender.Send(new GetPlanLookupQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new GetPlanByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Create(PlanRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePlanCommand(request.Name, request.Price!.Value, request.DurationInMonths!.Value,
            request.IsPublished, request.MaxFreezeDays ?? 0, request.MaxFreezes ?? 0, request.GuestPassQuota ?? 0,
            request.AccessScope ?? AccessScope.HomeBranchOnly), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : this.ToProblem(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.Manager)]
    public async Task<IActionResult> Update(Guid id, PlanRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await sender.Send(new UpdatePlanCommand(id, request.Name, request.Price!.Value, request.DurationInMonths!.Value,
            request.IsPublished, request.MaxFreezeDays ?? 0, request.MaxFreezes ?? 0, request.GuestPassQuota ?? 0,
            request.AccessScope ?? AccessScope.HomeBranchOnly), cancellationToken));
}
