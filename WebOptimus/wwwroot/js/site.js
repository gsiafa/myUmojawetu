// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//let inactivityTimeout;

//function resetTimer() {
//    clearTimeout(inactivityTimeout);
//    inactivityTimeout = setTimeout(logoutUser, 30 * 60 * 1000); // 30 minutes
//}

//function logoutUser() {
//    // Call the logout API or redirect to the login page
//    window.location.href = "/Account/Logout";
//}

//// Reset timer on user actions
//window.onload = resetTimer;
//document.onmousemove = resetTimer;
//document.onkeypress = resetTimer;


let warningTimeout;

function resetTimer() {
    clearTimeout(warningTimeout);
    warningTimeout = setTimeout(() => {
        alert("You will be logged out in 5 mins due to inactivity.");
    }, 25 * 60 * 1000); // Warn after 25 minutes
}

window.onload = resetTimer;
document.onmousemove = resetTimer;
document.onkeypress = resetTimer;
