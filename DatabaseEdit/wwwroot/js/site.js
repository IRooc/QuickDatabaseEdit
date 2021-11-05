﻿function searchTable(event) {
    var search = event.target.value.toLowerCase();
    var el = document.getElementById('tablerows');
    var rows = el.querySelectorAll('tr.clickable');
    if (search == '') {
        for (let i = 0; i < el.querySelectorAll('tr.clickable.hide').length; i++) {
            var row = rows[i];
            row.classList.remove('hide');
        }

    } else {
        for (let i = 0; i < rows.length; i++) {
            var row = rows[i];
            if (row.textContent.toLowerCase().indexOf(search) >= 0) {
                row.classList.remove('hide');

            } else {
                row.classList.add('hide');
            }
        }
    }
}

var sorters = document.querySelectorAll('th.sort');
for (let i = 0; i < sorters.length; i++) {
    var sort = sorters[i];
    sort.addEventListener('click', (e) => {
        console.log('clicked', e, e.target.cellIndex, e.target.parentElement.parentElement.parentElement);
        document.body.style.cursor = "wait";
        setTimeout(() => {
            sortTable(e.target.parentElement.parentElement.parentElement, e.target.cellIndex);
            document.body.style.cursor = "default";
        },100)
    })
}

var msgButton = document.querySelector('.message button');
if (msgButton) {
    msgButton.addEventListener("click", (e) => { e.target.parentElement.remove(); });
}

/*FROM w3cschools*/
function sortTable(table, n) {
    var rows, switching, i, x, y, shouldSwitch, dir, switchcount = 0;
    switching = true;
    // Set the sorting direction to ascending:
    dir = "asc";
    /* Make a loop that will continue until
    no switching has been done: */
    while (switching) {
        // Start by saying: no switching is done:
        switching = false;
        rows = table.rows;
        /* Loop through all table rows (except the
        first, which contains table headers): */
        for (i = 1; i < (rows.length - 1); i++) {
            // Start by saying there should be no switching:
            shouldSwitch = false;
            /* Get the two elements you want to compare,
            one from current row and one from the next: */
            x = rows[i].getElementsByTagName("TD")[n];
            y = rows[i + 1].getElementsByTagName("TD")[n];
            /* Check if the two rows should switch place,
            based on the direction, asc or desc: */
            if (dir == "asc") {
                if (x.innerHTML.toLowerCase() > y.innerHTML.toLowerCase()) {
                    // If so, mark as a switch and break the loop:
                    shouldSwitch = true;
                    break;
                }
            } else if (dir == "desc") {
                if (x.innerHTML.toLowerCase() < y.innerHTML.toLowerCase()) {
                    // If so, mark as a switch and break the loop:
                    shouldSwitch = true;
                    break;
                }
            }
        }
        if (shouldSwitch) {
            /* If a switch has been marked, make the switch
            and mark that a switch has been done: */
            rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
            switching = true;
            // Each time a switch is done, increase this count by 1:
            switchcount++;
        } else {
            /* If no switching has been done AND the direction is "asc",
            set the direction to "desc" and run the while loop again. */
            if (switchcount == 0 && dir == "asc") {
                dir = "desc";
                switching = true;
            }
        }
    }
}