// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Host.Bindings
{
    /// <summary>
    /// @ToDo
    /// 
    /// We need to remove this later. This iface will be included into the WebJobs SDK.
    /// Its semantics are described here:
    /// https://github.com/Azure/azure-webjobs-sdk/issues/2450
    /// 
    /// When an WebJobs SDK version with this iface is available, we will remove it frm here.
    /// For now, this ensures that we can code against this iface.
    /// </summary>
    public interface IErrorAwareValueBinder : IValueBinder
    {
        /// <summary>
        /// Sets the error.
        /// </summary>
        /// <param name="value">The value / state.</param>
        /// <param name="error">The error thrown.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
        /// <returns>A <see cref="Task"/> for the operation.</returns>
        Task SetErrorAsync(object value, Exception error, CancellationToken cancellationToken);
    }
}
