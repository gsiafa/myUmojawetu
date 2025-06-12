$(document).ready(function () {
    var dataTable = $('#myTable').DataTable({
        "paging": true,
        "ordering": true,
        "info": true,
        "responsive": true
    });

    $('#applyFilters').on('click', function () {
        var ageFilter = $('#ageFilter').val();
        var regionFilter = $('#regionFilter').val();

        dataTable.columns().search('').draw(); // Clear all column searches first

        if (ageFilter === 'Under18') {
            dataTable.columns(5).search('^([0-1][0-9]|2[0-4])$', true, false).draw();
        } else if (ageFilter === 'Over18') {
            dataTable.columns(5).search('^([2-9][5-9]|[3-9][0-9]|[1-9][0-9]{2,})$', true, false).draw();
        }

        if (regionFilter) {
            dataTable.columns(3).search(regionFilter).draw();
        }
    });
});
