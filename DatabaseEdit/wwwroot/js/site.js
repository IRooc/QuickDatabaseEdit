var currentQuerystring = document.location.search;
function searchTable(event) {
    var search = event.target.value.toLowerCase();
    localStorage.setItem('search' + currentQuerystring, search);
    var el = document.getElementById('tablerows');
    var rows = el.querySelectorAll('tr.clickable');
    //console.log('search', search);
    if (search == '') {
        var hidden = el.querySelectorAll('tr.clickable.hide')
        for (let i = 0; i < hidden.length; i++) {
            var row = hidden[i];
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
var curSearch = localStorage.getItem('search' + currentQuerystring);
var search = document.getElementById('search');
if (curSearch && search) {
    search.value = curSearch;
    var event = new Event('input', {
        bubbles: true,
        cancelable: true,
    });
    search.dispatchEvent(event);
}

var sorters = document.querySelectorAll('th.sort');
for (let i = 0; i < sorters.length; i++) {
    var sort = sorters[i];
    sort.addEventListener('click', (e) => {
        document.body.style.cursor = "wait";
        setTimeout(() => {
            sortTable(e.target.parentElement.parentElement.parentElement, e.target.cellIndex, e.target.dataset.type);
            document.body.style.cursor = "default";
        }, 100)
    })
}

var msgButton = document.querySelector('.message button');
if (msgButton) {
    msgButton.addEventListener("click", (e) => { e.target.parentElement.remove(); });
}

async function newguid(event) {
    const response = await fetch('?handler=Guid');
    event.target.parentElement.querySelector('input').value = (await response.text()).substring(1,37);
}


/*FROM w3cschools*/
/*FROM w3cschools https://www.w3schools.com/howto/howto_js_sort_table.asp*/
/*FROM w3cschools with data-type tweaks*/
function sortTable(table, n, datatype) {
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

            if (datatype == 'int32') {
                x = parseInt(x.innerHTML.toLowerCase());
                y = parseInt(y.innerHTML.toLowerCase());

            } else {
                x = x.innerHTML.toLowerCase();
                y = y.innerHTML.toLowerCase();
            }

            /* Check if the two rows should switch place,
            based on the direction, asc or desc: */
            if (dir == "asc") {
                if (x > y) {
                    // If so, mark as a switch and break the loop:
                    shouldSwitch = true;
                    break;
                }
            } else if (dir == "desc") {
                if (x < y) {
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