/**
 * Kanban.js - Interactive Kanban Board Logic
 */
const Kanban = {
    init: function (columns) {
        const statusMap = {
            'pending-tasks': 0,    // Pending
            'inprogress-tasks': 1, // InProgress
            'completed-tasks': 2   // Completed
        };

        columns.forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                Sortable.create(el, {
                    group: 'kanban',
                    animation: 150,
                    ghostClass: 'bg-primary/5',
                    onStart: function () { window.isDragging = true; },
                    onEnd: function (evt) {
                        setTimeout(() => { window.isDragging = false; }, 100);
                        const taskId = evt.item.getAttribute('data-task-id');
                        const newStatus = statusMap[evt.to.id];

                        if (taskId && newStatus !== undefined) {
                            this.updateTaskStatus(taskId, newStatus);
                        }
                    }.bind(this)
                });
            }
        });
    },

    updateTaskStatus: function (taskId, status) {
        const data = new URLSearchParams();
        data.append('taskId', taskId);
        data.append('statusId', status);

        fetch('/TodoTasks/UpdateStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: data
        }).then(response => {
            if (!response.ok) {
                showToast('Errore', 'Impossibile aggiornare lo stato', 'error');
                setTimeout(() => location.reload(), 1000);
            } else {
                showToast('Successo', 'Stato task aggiornato');
            }
        }).catch(err => {
            console.error('Update status error:', err);
            showToast('Errore', 'Errore di connessione', 'error');
        });
    }
};

window.Kanban = Kanban;
