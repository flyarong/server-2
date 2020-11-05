﻿using Bit.Core.Models.Table;
using Bit.Core.Repositories;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bit.Core.Services;
using System.Linq;
using Bit.Core.Identity;
using Microsoft.Extensions.Logging;
using IdentityServer4.Extensions;

namespace Bit.Core.IdentityServer
{
    public class CustomTokenRequestValidator : BaseRequestValidator<CustomTokenRequestValidationContext>,
        ICustomTokenRequestValidator
    {
        private UserManager<User> _userManager;

        public CustomTokenRequestValidator(
            UserManager<User> userManager,
            IDeviceRepository deviceRepository,
            IDeviceService deviceService,
            IUserService userService,
            IEventService eventService,
            IOrganizationDuoWebTokenProvider organizationDuoWebTokenProvider,
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository,
            IApplicationCacheService applicationCacheService,
            IMailService mailService,
            ILogger<ResourceOwnerPasswordValidator> logger,
            CurrentContext currentContext,
            GlobalSettings globalSettings,
            IPolicyRepository policyRepository)
            : base(userManager, deviceRepository, deviceService, userService, eventService,
                  organizationDuoWebTokenProvider, organizationRepository, organizationUserRepository,
                  applicationCacheService, mailService, logger, currentContext, globalSettings, policyRepository)
        {
            _userManager = userManager;
        }

        public async Task ValidateAsync(CustomTokenRequestValidationContext context)
        {
            if (context.Result.ValidatedRequest.GrantType != "authorization_code")
            {
                return;
            }
            await ValidateAsync(context, context.Result.ValidatedRequest);
        }

        protected async override Task<(User, bool)> ValidateContextAsync(CustomTokenRequestValidationContext context)
        {
            var user = await _userManager.FindByEmailAsync(context.Result.ValidatedRequest.Subject.GetDisplayName());
            return (user, user != null);
        }

        protected override void SetSuccessResult(CustomTokenRequestValidationContext context, User user,
            List<Claim> claims, Dictionary<string, object> customResponse)
        {
            context.Result.CustomResponse = customResponse;
            if (claims?.Any() ?? false)
            {
                context.Result.ValidatedRequest.Client.AlwaysSendClientClaims = true;
                context.Result.ValidatedRequest.Client.ClientClaimsPrefix = string.Empty;
                foreach (var claim in claims)
                {
                    context.Result.ValidatedRequest.ClientClaims.Add(claim);
                }
            }
        }

        protected override void SetTwoFactorResult(CustomTokenRequestValidationContext context,
            Dictionary<string, object> customResponse)
        {
            context.Result.Error = "invalid_grant";
            context.Result.ErrorDescription = "Two factor required.";
            context.Result.IsError = true;
            context.Result.CustomResponse = customResponse;
        }

        protected override void SetSsoResult(CustomTokenRequestValidationContext context, 
            Dictionary<string, object> customResponse) => throw new System.NotImplementedException();

        protected override void SetErrorResult(CustomTokenRequestValidationContext context,
            Dictionary<string, object> customResponse)
        {
            context.Result.Error = "invalid_grant";
            context.Result.IsError = true;
            context.Result.CustomResponse = customResponse;
        }
    }
}
