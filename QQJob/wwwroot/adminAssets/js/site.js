
// When the page has fully loaded, stop Pace (if not automatically stopped)
$(window).on('load', function () {
    Pace.stop();  // Stop Pace loading indicator
});
$(document).ready(function () {
    Pace.stop();  // Stop the Pace loading indicator when the page is fully loaded
});

function showDeleteAccountModel() {
    $.ajax({
        url: '/Admin/User/Delete',
        method: 'GET',
        contentType: 'application/json',
        success: function (response) {
            $("#modalPlaceholder").html(response);
            $("#deleteModal").modal("show");
        },
        error: function () {
            alert("An error occurred while processing your request.");
        }
    });
}
