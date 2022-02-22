import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { FileBrowser } from './components/FileBrowser/FileBrowser';

import './custom.css'

export default class App extends Component {
  static displayName = App.name;

  render () {
    return (
      <Layout>
        <Route exact path='/' component={FileBrowser} />
      </Layout>
    );
  }
}
