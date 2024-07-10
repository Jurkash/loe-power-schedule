using LoePowerSchedule.DAL;
using LoePowerSchedule.Models;

namespace LoePowerSchedule.Controllers;

using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController(ScheduleRepository scheduleRepository)
    : ControllerBase
{
    [HttpGet("all")]
    [ProducesResponseType(typeof(List<ScheduleDoc>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSchedules()
    {
        var schedules = await scheduleRepository.GetAllAsync();
        return Ok(schedules);
    }

    [HttpGet("{date}")]
    [ProducesResponseType(typeof(ScheduleDoc), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleByDate(DateTimeOffset date)
    {
        var schedule = await scheduleRepository.GetByDateAsync(date);
        return Ok(schedule);
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(ScheduleDoc), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestSchedule()
    {
        var schedule = await scheduleRepository.GetLatestAsync();
        return Ok(schedule);
    }
    
    [HttpGet("next-power-off/{groupId}")]
    [ProducesResponseType(typeof(DateTimeOffset), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNextPowerOff(string groupId)
    {
        var nextStateTime = await scheduleRepository.GetNextStateTime(groupId, GridState.PowerOff);
        return Ok(nextStateTime);
    }
    
    [HttpGet("next-power-on/{groupId}")]
    [ProducesResponseType(typeof(DateTimeOffset), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNextPowerOn(string groupId)
    {
        var nextStateTime = await scheduleRepository.GetNextStateTime(groupId, GridState.PowerOn);
        return Ok(nextStateTime);
    }
}
