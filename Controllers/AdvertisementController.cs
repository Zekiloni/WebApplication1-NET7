﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAds.Interfaces;
using MyAds.Entities;
using MyAds.Models;
using System.Net;
using ClassyAdsServer.Models;

namespace MyAds.Controllers
{
    [ApiController]
    public class AdvertisementController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IAdvertisementService _advertisements;
        private readonly IUserService _users;
        private readonly IAdvertisementMediaService _advertisementMedia;

        public AdvertisementController(IAdvertisementService advertisements, IUserService users, IAdvertisementMediaService media, IConfiguration config)
        {
            _configuration = config;
            _users = users;
            _advertisements = advertisements;
            _advertisementMedia = media;
        }

        [HttpPost("/advertisements/search")]
        public async Task<IActionResult> SearchAdvertisements(AdvertisementSearchInput searchAdvertisement)
        {
            var advertisements = await _advertisements.GetAdvertisementsByFilter(searchAdvertisement.Filter, searchAdvertisement.CategoryId);

            var totalNumberOfRecords = advertisements.Count();
            var totalNumberOfPages = (int)Math.Ceiling((double)totalNumberOfRecords / searchAdvertisement.PageSize);

            var advertisementsCurrentPage = advertisements
                .Skip((searchAdvertisement.PageNumber - 1) * searchAdvertisement.PageSize)
                .Take(searchAdvertisement.PageSize);

            var pagedOutput = new PagedOutput<Advertisement>
            {
                PageNumber = searchAdvertisement.PageNumber,
                PageSize = searchAdvertisement.PageSize,
                TotalNumberOfPages = totalNumberOfPages,
                TotalNumberOfRecords = totalNumberOfRecords,
                Results = advertisementsCurrentPage
            };

            return Ok(pagedOutput);
        }

        [HttpGet("/advertisements/{advertisementId}")]
        [Authorize]
        public async Task<IActionResult> GetAdvertisementById(int advertisementId)
        {
            var advertisement = await _advertisements.GetAdvertisementById(advertisementId);

            if (advertisement == null)
            {
                return StatusCode((int)HttpStatusCode.NotFound, new ErrorResponse("Advertisement not found.", "Advertisement may be not active or is deleted."));
            }

            return Ok(advertisement);
        }

        [HttpPost("/advertisements/create")]
        [Authorize]
        public async Task<IActionResult> CreateAdvertisement(NewAdvertisementInput newAdvertisement)
        {
            var userAuthorId = (int)HttpContext.Items["UserId"]!;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var Advertisement = new Advertisement
                {
                    CategoryId = newAdvertisement.CategoryId,
                    Title = newAdvertisement.Title,
                    ShortDescription = newAdvertisement.ShortDescription,
                    Description = newAdvertisement.Description,
                    UserId = userAuthorId
                };

                if (Advertisement == null)
                {
                    return BadRequest();
                }

                await _advertisements.CreateAdvertisement(Advertisement);

                foreach (var file in newAdvertisement.MediaFiles)
                {
                    var mediaFile = new AdvertisementMediaFile
                    {
                        AdvertisementId = Advertisement.Id,
                        Url = await _advertisementMedia.UploadMediaFile(file)
                    };

                    if (mediaFile != null)
                    {
                        await _advertisementMedia.CreateMediaFile(mediaFile);
                    }
                }
                return Ok(Advertisement);
            }
            catch (Exception errorCreating)
            {
                return BadRequest(errorCreating);
            }
        }


        [HttpDelete("/advertisements/delete/{advertisementId}")]
        [Authorize]
        public async Task<IActionResult> DeleteAdvertisement(int advertisementId)
        {
            var user = await _users.GetUserById((int)HttpContext.Items["UserId"]!);
            var advertisement = await _advertisements.GetAdvertisementById(advertisementId);

            if (advertisement == null || user == null)
            {
                return NotFound();
            }

            if (advertisement.UserId != user.Id && user.Role < Enums.UserRole.Admin)
            {
                return Unauthorized();
            }

            await _advertisements.DeleteAdvertisement(advertisement);

            return Ok();
        }
    }
}
