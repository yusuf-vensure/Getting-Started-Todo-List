// client/src/app/app.ts
import { Component, OnInit, signal, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { ApiService } from './api.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, RouterOutlet],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class AppComponent implements OnInit {
  title = signal('getting-started-todo-app');
  
  userName: string = '';
  viewCount: number | string = 'Loading...';

  private apiService = inject(ApiService);

  ngOnInit() {
    this.fetchStats();
  }

  fetchStats() {
    this.apiService.getStats().subscribe({
      next: (data) => {
        // This maps the JSON key from your C# API directly to your frontend variable
        this.viewCount = data.total_page_loads; 
      },
      error: (err) => console.error('Error fetching views:', err)
    });
  }

  onSubmit() {
    if (!this.userName) return;

    this.apiService.addUser(this.userName).subscribe({
      next: (response) => {
        alert(response); 
        this.userName = ''; 
      },
      error: (err) => console.error('Error saving user:', err)
    });
  }
}